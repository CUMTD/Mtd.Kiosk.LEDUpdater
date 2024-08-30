using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mtd.Kiosk.IpDisplaysApi;
using Mtd.Kiosk.LedUpdater.Realtime;
using Mtd.Kiosk.LedUpdater.SanityClient;
using Mtd.Kiosk.LedUpdater.SanityClient.Schema;
using Mtd.Kiosk.LedUpdater.Service;

namespace Mtd.Kiosk.LEDUpdater.Service;

internal abstract class LedSignBackgroundService : BackgroundService, IDisposable
{
	protected readonly List<KioskDocument> _kiosks = [];
	protected readonly LedUpdaterServiceConfig _config;
	protected readonly RealtimeClient _realtimeClient;
	protected readonly IpDisplaysApiClientFactory _ipDisplaysAPIClientFactory;
	protected readonly SanityClient _sanityApiClient;
	protected readonly Dictionary<string, LedSign> _signs = [];
	protected readonly ILogger<LedSignBackgroundService> _logger;

	protected LedSignBackgroundService(IOptions<LedUpdaterServiceConfig> config, RealtimeClient realtimeClient, IpDisplaysApiClientFactory ipDisplaysClientFactory, SanityClient sanityApiClient, ILogger<LedSignBackgroundService> logger)
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

	protected override Task ExecuteAsync(CancellationToken stoppingToken) => Run(stoppingToken);

	public override async Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("{service} started.", GetType().Name);

		// fetch kiosks with LED signs from Sanity
		var kiosks = await GetKiosksAsync(cancellationToken); // will not return null

		_kiosks.AddRange(kiosks);

		// create a sign client for each IP address
		foreach (var kiosk in kiosks)
		{
			_signs.Add(kiosk.Id, new LedSign(kiosk.Id, _ipDisplaysAPIClientFactory.CreateClient(kiosk.Id, kiosk.LedIp), _logger));
		}

		await base.StartAsync(cancellationToken);
	}

	public override Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("{service} stopped.", GetType().Name);
		return base.StopAsync(cancellationToken);
	}

	protected abstract Task Run(CancellationToken cancellationToken);

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
