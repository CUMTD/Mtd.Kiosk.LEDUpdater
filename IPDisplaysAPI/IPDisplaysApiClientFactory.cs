using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mtd.Kiosk.LEDUpdater.IpDisplaysApi;

public class IPDisplaysApiClientFactory
{
	private readonly IOptions<IPDisplaysApiClientConfig> _config;
	private readonly ILogger<IPDisplaysApiClient> _logger;

	public IPDisplaysApiClientFactory(IOptions<IPDisplaysApiClientConfig> config, ILogger<IPDisplaysApiClient> logger)
	{
		ArgumentNullException.ThrowIfNull(config?.Value);
		ArgumentNullException.ThrowIfNull(logger);

		_config = config;
		_logger = logger;
	}

	public IPDisplaysApiClient CreateClient(string ipAddress)
	{
		return new IPDisplaysApiClient(ipAddress, _config, _logger);
	}
}
