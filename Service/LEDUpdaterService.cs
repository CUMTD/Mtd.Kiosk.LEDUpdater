using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mtd.Kiosk.LEDUpdater.IpDisplaysApi;
using Mtd.Kiosk.LEDUpdater.Realtime;
using Mtd.Kiosk.LEDUpdater.Realtime.Entitites;
using Mtd.Kiosk.LEDUpdater.SanityApi;
using Mtd.Kiosk.LEDUpdater.SanityApi.Schema;


namespace Mtd.Kiosk.LEDUpdater.Service;
internal class LedUpdaterService : BackgroundService, IDisposable
{
	private readonly RealtimeClient _realtimeClient;
	private readonly IPDisplaysApiClientFactory _ipDisplaysAPIClientFactory;
	private readonly SanityClient _sanityApiClient;
	private readonly ILogger<LedUpdaterService> _logger;
	private readonly Dictionary<string, LedSign> _signs;

	public LedUpdaterService(RealtimeClient realtimeClient, IPDisplaysApiClientFactory clientFactory, SanityClient sanityApiClient, ILogger<LedUpdaterService> logger)
	{
		ArgumentNullException.ThrowIfNull(realtimeClient, nameof(realtimeClient));
		ArgumentNullException.ThrowIfNull(clientFactory, nameof(clientFactory));
		ArgumentNullException.ThrowIfNull(sanityApiClient, nameof(sanityApiClient));
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));

		_realtimeClient = realtimeClient;
		_ipDisplaysAPIClientFactory = clientFactory;
		_sanityApiClient = sanityApiClient;
		_logger = logger;
		_signs = [];
	}

	protected async override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("LED Updater Service started.");
		// fetch kiosks with LED signs from Sanity
		var kiosks = await GetKiosksAsync(stoppingToken);
		var kioskDictionary = kiosks.ToDictionary(k => k.Id, k => k);
		_logger.LogInformation("Fetched {count} kiosks with LED signs from Sanity.", kiosks.Count);

		// each kiosk will be mapped to a stack of departures
		Dictionary<string, Stack<Departure>> departuresDictionary = [];

		// create a sign client for each IP address
		foreach (var kiosk in kiosks)
		{
			_signs.Add(kiosk.Id, new LedSign(_ipDisplaysAPIClientFactory.CreateClient(kiosk.LedIp)));
			departuresDictionary.Add(kiosk.Id, new Stack<Departure>());

			// fill this kiosk's departures stack
			var departures = await FetchDepartures(kiosk.StopId, stoppingToken);
			if (departures == null) // fetch fails
			{
				_logger.LogError("Failed to fetch departures for {kioskName} ({kioskId}).", kiosk.DisplayName, kiosk.Id);
				await _signs[kiosk.Id].BlankScreen();
				continue;
			}
			departuresDictionary[kiosk.Id] = new Stack<Departure>(departures);
			_logger.LogDebug("Updated {kioskName} ({kioskId}) with {count} departures.", kiosk.DisplayName, kiosk.Id, departures.Count);

		}

		// main loop
		while (!stoppingToken.IsCancellationRequested)
		{
			var activeMessages = await FetchGeneralMessages(stoppingToken);
			if (activeMessages == null)
			{
				_logger.LogError("Failed to fetch general messages.");
				activeMessages = [];
			}

			// send updates to each sign
			foreach (var kioskIdKey in departuresDictionary.Keys)
			{
				var currentKiosk = kioskDictionary[kioskIdKey];
				var departuresStack = departuresDictionary[kioskIdKey];

				Departure topDeparture;
				Departure bottomDeparture;

				// refill the stack if empty
				if (departuresStack.Count == 0)
				{
					_logger.LogTrace("No departures left in stack. Fetching...");
					var departures = await FetchDepartures(currentKiosk.StopId, stoppingToken);

					if (departures == null) // fetch fails, blank the screen
					{
						_logger.LogError("Failed to fetch departures for {kioskName} ({kioskId}).", currentKiosk.DisplayName, currentKiosk.Id);
						await _signs[kioskIdKey].BlankScreen();
						continue;
					}

					foreach (var departure in departures)
					{
						departuresStack.Push(departure);
					}

				}

				// normal operation
				var kioskActiveMessages = activeMessages.Where(m => m.StopId == currentKiosk.StopId);
				if (kioskActiveMessages.Any()) // check for active messages for this kiosk
				{
					if (kioskActiveMessages.Any(m => m.BlockRealtime) || departuresStack.Count == 0) // the message blocks realtime
					{
						_logger.LogTrace("Showing fullscreen message for {kioskName} ({kioskId}).", currentKiosk.DisplayName, currentKiosk.Id);
						var message = activeMessages.First(m => m.StopId == currentKiosk.StopId);
						await _signs[kioskIdKey].UpdateTwoLineMessage(message.Message, "");
					}
					else
					{
						// the message occupies one line
						_logger.LogTrace("Showing one-line message for {kioskName} ({kioskId}).", currentKiosk.DisplayName, currentKiosk.Id);
						bottomDeparture = departuresStack.Pop();
						var message = activeMessages.First(m => m.StopId == currentKiosk.StopId);
						await _signs[kioskIdKey].UpdateOneLineMessage(message.Message, bottomDeparture);
					}
				}
				else
				{ // no active messages
					if (departuresStack.Count == 0) // refill stack if empty
					{
						_logger.LogWarning("No departures found for {kioskName} ({kioskId}).", currentKiosk.DisplayName, currentKiosk.Id);
						await _signs[kioskIdKey].UpdateTwoLineMessage("No departures at this time.", "");
						continue;
					}
					else if (departuresStack.Count == 1) // only one departure left
					{
						_logger.LogTrace("Showing one departure for {kioskName} ({kioskId}).", currentKiosk.DisplayName, currentKiosk.Id);
						topDeparture = departuresStack.Pop();
						await _signs[kioskIdKey].UpdateTwoLineDepartures(topDeparture, null);
						continue;
					}
					// regular two line operation
					_logger.LogTrace("Showing departures for {kioskName} ({kioskId}).", currentKiosk.DisplayName, currentKiosk.Id);
					bottomDeparture = departuresStack.Pop();
					topDeparture = departuresStack.Pop();
					await _signs[kioskIdKey].UpdateTwoLineDepartures(topDeparture, bottomDeparture);
				}
			}
			// TODO: Add ServiceConfigObject with configurable update interval
			await Task.Delay(6_000, stoppingToken);
		}
	}

	private async Task<IReadOnlyCollection<Departure>?> FetchDepartures(string stopId, CancellationToken cancellationToken)
	{
		try
		{
			var departures = await _realtimeClient.GetDeparturesForStopIdAsync(stopId, cancellationToken);
			_logger.LogDebug("Fetched {count} departures for {stopId}", departures.Count, stopId);
			return departures;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch departures for {stopId}", stopId);
		}
		return null;


	}

	private async Task<IReadOnlyCollection<GeneralMessage>?> FetchGeneralMessages(CancellationToken cancellationToken)
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
		return null;
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

		return kiosks;
	}

}
