using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mtd.Kiosk.LEDUpdater.IPDisplaysAPI;

namespace Mtd.Kiosk.LEDUpdater.Service;
internal class LEDUpdaterService : BackgroundService, IDisposable
{
	private readonly IpDisplaysApiClient _client;
	private readonly ILogger<LEDUpdaterService> _logger;

	public LEDUpdaterService(IpDisplaysApiClient client, ILogger<LEDUpdaterService> logger)
	{
		ArgumentNullException.ThrowIfNull(client, nameof(client));
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));

		_client = client;
		_logger = logger;
	}

	protected async override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var client = _client.GetSoapClient();
		var response = await client.GetLayoutsAsync(new GetLayoutsRequest());

		_logger.LogInformation("Got XML: {xml} ", response.layoutInfoXml);
	}


}
