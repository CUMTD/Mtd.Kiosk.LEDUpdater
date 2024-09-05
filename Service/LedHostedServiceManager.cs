using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mtd.Kiosk.LedUpdater.SanityClient;

namespace Mtd.Kiosk.LedUpdater.Service;
internal class LedHostedServiceManager : BackgroundService, IHostedService, IDisposable
{
	private readonly IServiceProvider _serviceProvider;
	private readonly SanityApiClient _sanityApiClient;
	private readonly ILogger<LedHostedServiceManager> _logger;
	private readonly List<LedDepartureUpdaterService> _hostedServices;

	/// <summary>
	/// This service fetches all kiosks and bootstraps a LedDepartureUpdaterService for each kiosk.
	/// It will ensure that all services run in thier own threas and are disposed of when the service is stopped.
	/// </summary>
	/// <param name="serviceProvider"></param>
	/// <param name="sanityApiClient"></param>
	/// <param name="logger"></param>
	public LedHostedServiceManager(IServiceProvider serviceProvider, SanityApiClient sanityApiClient, ILogger<LedHostedServiceManager> logger)
	{
		ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));
		ArgumentNullException.ThrowIfNull(sanityApiClient, nameof(sanityApiClient));
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));

		_serviceProvider = serviceProvider;
		_sanityApiClient = sanityApiClient;
		_logger = logger;

		_hostedServices = [];
	}

	protected override async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		// Fetch kiosks with LED signs from Sanity
		var kiosks = await _sanityApiClient.GetKiosks(cancellationToken);

		foreach (var kiosk in kiosks)
		{
			// Create scope but do not dispose it immediately
			var scope = _serviceProvider.CreateScope();

			// Get the LedDepartureUpdaterService from the scope
			// And set the kiosk to the current kiosk in the loop
			var service = scope.ServiceProvider.GetRequiredService<LedDepartureUpdaterService>();
			service.SetKiosk(kiosk);

			// Add the service to the list of hosted services
			// so we can stop and dispose them later
			_hostedServices.Add(service);

			// Start the service
			await service.StartAsync(cancellationToken);
		}

		// Keep the service alive
		try
		{
			await Task.Delay(Timeout.Infinite, cancellationToken);
		}
		catch (TaskCanceledException)
		{
			// Expected when the service is stopped
		}
	}

	public override async Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("{service} starting.", GetType().Name);

		await base.StartAsync(cancellationToken);
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Stopping {service}", nameof(LedHostedServiceManager));

		// Stop services and dispose
		var stopTasks = _hostedServices.Select(async service =>
		{
			await service.StopAsync(cancellationToken);
			if (service is IDisposable disposableService)
			{
				disposableService.Dispose();
			}
		});

		await Task.WhenAll(stopTasks);

		_logger.LogInformation("All services stopped");

		await base.StopAsync(cancellationToken);
	}

	public override void Dispose()
	{
		foreach (var service in _hostedServices)
		{
			service.Dispose();
		}

		_hostedServices.Clear();

		base.Dispose();
	}
}
