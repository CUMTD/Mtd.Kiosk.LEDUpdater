using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mtd.Kiosk.IpDisplaysApi;
using Mtd.Kiosk.LedUpdater.Realtime;

namespace Mtd.Kiosk.LedUpdater.Service;
internal class LedBrightnessService : BackgroundService, IHostedService, IDisposable
{
	private readonly LedUpdaterServiceConfig _config;
	private readonly RealtimeClient _realtimeClient;
	private readonly IpDisplaysApiClientFactory _ipDisplaysAPIClientFactory;
	private readonly SanityClient.SanityApiClient _sanityApiClient;
	private readonly ILogger<LedBrightnessService> _logger;

	public LedBrightnessService(IOptions<LedUpdaterServiceConfig> config, RealtimeClient realtimeClient, IpDisplaysApiClientFactory ipDisplaysClientFactory, SanityClient.SanityApiClient sanityApiClient, ILogger<LedBrightnessService> logger)
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
		_logger = logger;
	}

	public override async Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("{service} started.", nameof(LedDepartureUpdaterService));
		await base.StartAsync(cancellationToken);
	}

	public override Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("{service} stopped.", nameof(LedDepartureUpdaterService));
		return base.StopAsync(cancellationToken);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// Fetch kiosks with LED signs from Sanity
		var kiosks = await _sanityApiClient.GetKiosks(stoppingToken);
		var signs = new Dictionary<string, LedSign>();

		// create a sign client for each IP address
		foreach (var kiosk in kiosks)
		{
			signs.Add(kiosk.Id, new LedSign(kiosk.Id, _ipDisplaysAPIClientFactory.CreateClient(kiosk.LedIp, kiosk.Id), _logger));
		}

		// this will be checked and updated each time the loop runs
		// we will only update signs if the value has changed.
		var brightness = _config.LightModeBrightness;
		var brightnessUpdatePending = true;

		// main loop
		while (!stoppingToken.IsCancellationRequested)
		{
			var darkModeStatus = await FetchDarkModeStatus(stoppingToken);
			var newBrightness = darkModeStatus ? _config.DarkModeBrightness : _config.LightModeBrightness;
			if (brightness != newBrightness)
			{
				brightness = newBrightness;
				brightnessUpdatePending = true;
			}

			// send updates to each sign
			foreach (var kiosk in kiosks)
			{
				if (brightnessUpdatePending)
				{
					await signs[kiosk.Id].UpdateBrightness(brightness);
				}
			}

			// reset the flag
			brightnessUpdatePending = false;

			await Task.Delay(_config.BrightnessUpdateInterval, stoppingToken);
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
