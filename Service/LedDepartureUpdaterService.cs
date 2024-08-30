using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mtd.Kiosk.IpDisplaysApi;
using Mtd.Kiosk.IpDisplaysApi.Models;
using Mtd.Kiosk.LedUpdater.Realtime;
using Mtd.Kiosk.LedUpdater.Realtime.Entitites;
using Mtd.Kiosk.LedUpdater.SanityClient.Schema;
using Mtd.Kiosk.LEDUpdater.Service;

namespace Mtd.Kiosk.LedUpdater.Service;
internal class LedDepartureUpdaterService(IOptions<LedUpdaterServiceConfig> config, RealtimeClient realtimeClient, IpDisplaysApiClientFactory ipDisplaysClientFactory, SanityClient.SanityClient sanityApiClient, ILogger<LedDepartureUpdaterService> logger) :
	LedSignBackgroundService(config, realtimeClient, ipDisplaysClientFactory, sanityApiClient, logger), IDisposable
{
	protected override async Task Run(CancellationToken cancellationToken)
	{
		// each kiosk will be mapped to a stack of departures
		var departuresDictionary = _kiosks.ToDictionary(k => k.Id, _ => new Stack<Departure>());
		var kiosksDictionary = _kiosks.ToDictionary(k => k.Id, k => k);

		// populate departurs for each sign.
		foreach (var kiosk in _kiosks)
		{
			// fill this kiosk's departures stack
			var departures = await FetchDepartures(kiosk, cancellationToken);
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
		while (!cancellationToken.IsCancellationRequested)
		{
			var activeMessages = await FetchGeneralMessages(cancellationToken);

			// send updates to each sign
			foreach (var kioskIdKey in departuresDictionary.Keys)
			{
				var currentKiosk = kiosksDictionary[kioskIdKey];
				var departuresStack = departuresDictionary[kioskIdKey];

				// refill the stack if empty
				if (departuresStack.Count == 0)
				{
					_logger.LogTrace("No departures left in stack for {kioskName} ({kioskId}). Fetching...", currentKiosk.DisplayName, currentKiosk.Id);
					var departures = await FetchDepartures(currentKiosk, cancellationToken);

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
						await _signs[kioskIdKey].UpdateSign("No departures for at this time.", string.Empty);
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

			await Task.Delay(_config.SignUpdateInterval, cancellationToken);
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
}
