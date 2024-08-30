using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mtd.Kiosk.IpDisplaysApi;
using Mtd.Kiosk.LedUpdater.Realtime;
using Mtd.Kiosk.LEDUpdater.Service;

namespace Mtd.Kiosk.LedUpdater.Service;
internal class LedBrightnessService(IOptions<LedUpdaterServiceConfig> config, RealtimeClient realtimeClient, IpDisplaysApiClientFactory ipDisplaysClientFactory, SanityClient.SanityClient sanityApiClient, ILogger<LedBrightnessService> logger) :
	LedSignBackgroundService(config, realtimeClient, ipDisplaysClientFactory, sanityApiClient, logger), IDisposable
{

	protected override async Task Run(CancellationToken cancellationToken)
	{
		// this will be checked and updated each time the loop runs
		// we will only update signs if the value has changed.
		var brightness = _config.LightModeBrightness;
		var brightnessUpdatePending = true;

		// main loop
		while (!cancellationToken.IsCancellationRequested)
		{
			var darkModeStatus = await FetchDarkModeStatus(cancellationToken);
			var newBrightness = darkModeStatus ? _config.DarkModeBrightness : _config.LightModeBrightness;
			if (brightness != newBrightness)
			{
				brightness = newBrightness;
				brightnessUpdatePending = true;
			}

			// send updates to each sign
			foreach (var kiosk in _kiosks)
			{
				if (brightnessUpdatePending)
				{
					await _signs[kiosk.Id].UpdateBrightness(brightness);
				}
			}

			// reset the flag
			brightnessUpdatePending = false;

			await Task.Delay(_config.BrightnessUpdateInterval, cancellationToken);
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
			_logger.LogWarning(ex, "Failed to fetch dark mode status.");
			return false;
		}
	}
}
