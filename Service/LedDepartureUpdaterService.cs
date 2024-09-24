using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mtd.Kiosk.IpDisplaysApi;
using Mtd.Kiosk.IpDisplaysApi.Models;
using Mtd.Kiosk.LedUpdater.Realtime;
using Mtd.Kiosk.LedUpdater.Realtime.Entitites;
using Mtd.Kiosk.LedUpdater.SanityClient.Schema;

namespace Mtd.Kiosk.LedUpdater.Service;
internal class LedDepartureUpdaterService : BackgroundService, IHostedService, IDisposable
{
	private readonly Stack<Departure> _departuresStack;
	protected readonly LedUpdaterServiceConfig _config;
	protected readonly RealtimeClient _realtimeClient;
	protected readonly IpDisplaysApiClientFactory _ipDisplaysAPIClientFactory;
	protected readonly SanityClient.SanityApiClient _sanityApiClient;
	protected readonly Dictionary<string, LedSign> _signs = [];
	protected readonly ILogger<LedDepartureUpdaterService> _logger;

	public KioskDocument? Kiosk { get; private set; }

	public LedDepartureUpdaterService(
			IOptions<LedUpdaterServiceConfig> config,
			RealtimeClient realtimeClient,
			IpDisplaysApiClientFactory ipDisplaysClientFactory,
			SanityClient.SanityApiClient sanityApiClient,
			ILogger<LedDepartureUpdaterService> logger
		)
	{
		ArgumentNullException.ThrowIfNull(config?.Value, nameof(config));
		ArgumentNullException.ThrowIfNull(realtimeClient, nameof(realtimeClient));
		ArgumentNullException.ThrowIfNull(ipDisplaysClientFactory, nameof(ipDisplaysClientFactory));
		ArgumentNullException.ThrowIfNull(sanityApiClient, nameof(sanityApiClient));
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));

		_departuresStack = new Stack<Departure>();
		_config = config.Value;
		_realtimeClient = realtimeClient;
		_ipDisplaysAPIClientFactory = ipDisplaysClientFactory;
		_sanityApiClient = sanityApiClient;
		_logger = logger;
	}
	public void SetKiosk(KioskDocument kiosk) => Kiosk = kiosk;

	public override async Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("{service} started for {kioskName} ({kioskId}).", nameof(LedDepartureUpdaterService), Kiosk?.DisplayName, Kiosk?.Id);
		await base.StartAsync(cancellationToken);
	}

	public override Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("{service} stopped for {kioskName} ({kioskId}).", nameof(LedDepartureUpdaterService), Kiosk?.DisplayName, Kiosk?.Id);
		return base.StopAsync(cancellationToken);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if (Kiosk == null)
		{
			_logger.LogWarning("Could not start because kiosk has not been set.");
			return;
		}

		var sign = new LedSign(Kiosk.Id, _ipDisplaysAPIClientFactory.CreateClient(Kiosk.LedIp, Kiosk.Id), _logger);

		// main loop
		while (!stoppingToken.IsCancellationRequested)
		{
			var activeKioskMessage = await FetchGeneralMessages(stoppingToken);

			// refill the stack if empty
			if (_departuresStack.Count == 0)
			{
				_logger.LogTrace("No departures left in stack for {kioskName} ({kioskId}). Fetching...", Kiosk.DisplayName, Kiosk.Id);
				var updateResult = await UpdateDepartures(stoppingToken);
				if (!updateResult)
				{
					var wait = _config.SignUpdateInterval;
					_logger.LogInformation("Failed to fetch departures for {kioskName} ({kioskId}). Waiting {seconds}s and trying again.", Kiosk.DisplayName, Kiosk.Id, wait);
					await sign.BlankScreen();
					await Task.Delay(wait, stoppingToken);
					continue;
				}
			}

			bool successfullyUpdated = false;
			// normal operation
			if (activeKioskMessage != default) // check for active messages for this kiosk
			{
				if (activeKioskMessage.BlockRealtime || _departuresStack.Count == 0) // the message blocks realtime OR there are no departures so we need fullscreen
				{
					successfullyUpdated = await sign.UpdateSign(activeKioskMessage.Message, string.Empty);
				}
				else
				{
					// the message occupies one line
					var departure = _departuresStack.Pop();
					successfullyUpdated = await sign.UpdateSign(activeKioskMessage.Message, departure);
				}
			}
			else // no active messages
			{
				if (_departuresStack.Count == 0)
				{
					successfullyUpdated = await sign.UpdateSign("No departures for at this time.", string.Empty);
				}
				else if (_departuresStack.Count == 1) // only one departure left
				{
					var departure = _departuresStack.Pop();
					successfullyUpdated = await sign.UpdateSign(departure);
				}
				else
				{
					// regular two line operation
					var topDeparture = _departuresStack.Pop();
					var bottomDeparture = _departuresStack.Pop();
					successfullyUpdated = await sign.UpdateSign(topDeparture, bottomDeparture);
				}
			}

			if (successfullyUpdated)
			{
				await _realtimeClient.LogHeartbeat(Kiosk.Id, stoppingToken);
			}

			await Task.Delay(_config.SignUpdateInterval, stoppingToken);
		}
	}

	private async Task<bool> UpdateDepartures(CancellationToken cancellationToken)
	{
		var departures = await FetchDepartures(cancellationToken);

		if (departures == null) // fetch fails, blank the screen
		{
			_logger.LogWarning("Failed to fetch departures for {kioskName} ({kioskId}).", Kiosk?.DisplayName, Kiosk?.Id);
			return false;
		}

		_logger.LogDebug("Fetched {count} departures for {kioskName} ({kioskId}).", departures.Count, Kiosk?.DisplayName, Kiosk?.Id);
		foreach (var departure in departures)
		{
			_departuresStack.Push(departure);
		}

		return true;
	}

	private async Task<IReadOnlyCollection<Departure>?> FetchDepartures(CancellationToken cancellationToken)
	{
		if (Kiosk == null)
		{
			return default;
		}

		try
		{
			var departures = await _realtimeClient.GetDeparturesForStopIdAsync(Kiosk.StopId, Kiosk.Id, cancellationToken);

			return departures.Reverse().ToArray();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch departures for {stopId}", Kiosk.StopId);
		}

		return default;
	}

	private async Task<GeneralMessage?> FetchGeneralMessages(CancellationToken cancellationToken)
	{
		if (Kiosk == null)
		{
			return default;
		}

		try
		{
			var generalMessages = await _realtimeClient.GetActiveMessagesAsync(cancellationToken);
			_logger.LogDebug("Fetched {count} general messages.", generalMessages.Count);
			return generalMessages
				.Where(m => m.StopId == Kiosk.StopId)
				.OrderByDescending(m => m.BlockRealtime)
				.FirstOrDefault();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch general messages.");
		}

		return default;
	}
}
