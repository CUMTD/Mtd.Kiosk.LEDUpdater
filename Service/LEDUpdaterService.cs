using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mtd.Kiosk.LedUpdater.IpDisplaysApi;
using Mtd.Kiosk.LedUpdater.Realtime;
using Mtd.Kiosk.LedUpdater.Realtime.Entitites;
using Mtd.Kiosk.LedUpdater.SanityClient.Schema;

namespace Mtd.Kiosk.LedUpdater.Service;
internal class LedUpdaterService : BackgroundService, IDisposable
{
	private readonly LedUpdaterServiceConfig _config;
	private readonly RealtimeClient _realtimeClient;
	private readonly IpDisplaysApiClientFactory _ipDisplaysAPIClientFactory;
	private readonly SanityClient.SanityClient _sanityApiClient;
	private readonly Dictionary<string, LedSign> _signs;
	private readonly ILogger<LedUpdaterService> _logger;

	public LedUpdaterService(IOptions<LedUpdaterServiceConfig> config, RealtimeClient realtimeClient, IpDisplaysApiClientFactory ipDisplaysClientFactory, SanityClient.SanityClient sanityApiClient, ILogger<LedUpdaterService> logger)
	{
		ArgumentNullException.ThrowIfNull(config?.Value, nameof(config));
		ArgumentNullException.ThrowIfNull(realtimeClient, nameof(realtimeClient));
		ArgumentNullException.ThrowIfNull(ipDisplaysClientFactory, nameof(ipDisplaysClientFactory));
		ArgumentNullException.ThrowIfNull(sanityApiClient, nameof(sanityApiClient));
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));

		_config = config.Value;
		_realtimeClient = realtimeClient;
		_ipDisplaysAPIClientFactory = ipDisplaysClientFactory;
		_sanityApiClient = sanityApiClient;
		_signs = [];
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("LED Updater Service started.");

		// fetch kiosks with LED signs from Sanity
		var kiosks = await GetKiosksAsync(stoppingToken); // will not return null
		var kioskDictionary = kiosks.ToDictionary(k => k.Id, k => k);

		// each kiosk will be mapped to a stack of departures
		var departuresDictionary = kiosks.ToDictionary(k => k.Id, _ => new Stack<Departure>());

		// create a sign client for each IP address
		foreach (var kiosk in kiosks)
		{
			_signs.Add(kiosk.Id, new LedSign(_ipDisplaysAPIClientFactory.CreateClient(kiosk.LedIp), _logger));

			// fill this kiosk's departures stack
			var departures = await FetchDepartures(kiosk, stoppingToken);
			if (departures == null) // fetch fails
			{
				await _signs[kiosk.Id].BlankScreen();
			}
			else
			{
				departuresDictionary[kiosk.Id] = new Stack<Departure>(departures);
				_logger.LogDebug("Updated stack for {kioskName} ({kioskId}) with {count} departures.", kiosk.DisplayName, kiosk.Id, departures.Count);
			}
		}

		// main loop
		while (!stoppingToken.IsCancellationRequested)
		{
			var darkModeStatus = await FetchDarkModeStatus(stoppingToken);
			var activeMessages = await FetchGeneralMessages(stoppingToken);

			// send updates to each sign
			foreach (var kioskIdKey in departuresDictionary.Keys)
			{
				var currentKiosk = kioskDictionary[kioskIdKey];
				var departuresStack = departuresDictionary[kioskIdKey];

				await _signs[kioskIdKey].UpdateBrightness(darkModeStatus ? _config.DarkModeBrightness : _config.LightModeBrightness);

				// refill the stack if empty
				if (departuresStack.Count == 0)
				{
					_logger.LogTrace("No departures left in stack. Fetching...");
					var departures = await FetchDepartures(currentKiosk, stoppingToken);

					if (departures == null) // fetch fails, blank the screen
					{
						await _signs[kioskIdKey].BlankScreen();
						continue;
					}

					foreach (var departure in departures)
					{
						departuresStack.Push(departure);
					}
				}

				// normal operation

				var activeKioskMessage = activeMessages.Where(m => m.StopId == currentKiosk.StopId).OrderByDescending(m => m.BlockRealtime).FirstOrDefault();
				if (activeKioskMessage != default) // check for active messages for this kiosk
				{
					if (activeKioskMessage.BlockRealtime || departuresStack.Count == 0) // the message blocks realtime OR there are no departures so we need fullscreen
					{
						await _signs[kioskIdKey].UpdateSign(activeKioskMessage.Message, string.Empty);
					}
					else
					{
						// the message occupies one line
						var departure = departuresStack.Pop();
						await _signs[kioskIdKey].UpdateSign(activeKioskMessage.Message, departure);
					}
				}
				else // no active messages
				{
					if (departuresStack.Count == 0)
					{
						await _signs[kioskIdKey].UpdateSign("No departures at this time.", string.Empty);
					}
					else if (departuresStack.Count == 1) // only one departure left
					{
						var departure = departuresStack.Pop();
						await _signs[kioskIdKey].UpdateSign(departure);
					}
					else
					{
						// regular two line operation
						var topDeparture = departuresStack.Pop();
						var bottomDeparture = departuresStack.Pop();
						await _signs[kioskIdKey].UpdateSign(topDeparture, bottomDeparture);
					}
				}
			}

			await Task.Delay(_config.SignUpdateInterval, stoppingToken);
		}
	}

	private async Task<bool> FetchDarkModeStatus(CancellationToken stoppingToken)
	{
		try
		{
			var darkMode = await _realtimeClient.GetDarkModeStatus(stoppingToken);
			_logger.LogTrace("Dark mode status: {status}", darkMode);

			return darkMode;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch dark mode status.");
			return false;
		}
	}

	private async Task<IReadOnlyCollection<Departure>?> FetchDepartures(KioskDocument kiosk, CancellationToken cancellationToken)
	{
		try
		{
			var departures = await _realtimeClient.GetDeparturesForStopIdAsync(kiosk.StopId, kiosk.Id, cancellationToken);

			return departures.Reverse().ToArray();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch departures for {stopId}", kiosk.StopId);
		}

		return null;

	}

	private async Task<IReadOnlyCollection<GeneralMessage>> FetchGeneralMessages(CancellationToken cancellationToken)
	{
		try
		{
			var generalMessages = await _realtimeClient.GetActiveMessagesAsync(cancellationToken);
			_logger.LogDebug("Fetched {count} general messages.", generalMessages.Count);
			return generalMessages;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch general messages.");
		}

		return [];
	}

	private async Task<IReadOnlyCollection<KioskDocument>> GetKiosksAsync(CancellationToken cancellationToken)
	{
		IReadOnlyCollection<KioskDocument> kiosks;

		try
		{
			kiosks = await _sanityApiClient.GetKiosks(cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch Kiosks from Sanity API.");
			throw;
		}

		if (kiosks == null)
		{
			_logger.LogError("Sanity API completed successfully, but returned no kiosks.");
			throw new Exception("Sanity API completed successfully, but returned no kiosks.");
		}

		_logger.LogInformation("Fetched {count} kiosks with LED signs from Sanity.", kiosks.Count);
		return kiosks;
	}
}
