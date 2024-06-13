using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mtd.Kiosk.LEDUpdater.IPDisplays.API;
using ServiceReference1;

namespace Mtd.Kiosk.LEDUpdaterService.Service;
internal class LEDUpdaterService : BackgroundService, IDisposable
{
	private readonly ILogger<LEDUpdaterService> _logger;

	public LEDUpdaterService(ILogger<LEDUpdaterService> logger)
	{
		_logger = logger;
	}

	protected async override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		IpDisplaysApi soapClient = new IpDisplaysApi("10.128.17.35", _logger); // TODO: need to load this in from configuration

		var client = soapClient.GetClient();
		var request = new GetLayoutsRequest();

		var response = await client.GetLayoutsAsync(request);

		var layouts = response.layoutInfoXml;

		return;

	}


}
