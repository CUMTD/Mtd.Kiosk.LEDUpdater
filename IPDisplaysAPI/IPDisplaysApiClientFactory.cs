using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mtd.Kiosk.LedUpdater.IpDisplaysApi;

public class IpDisplaysApiClientFactory
{
	private readonly IOptions<IpDisplaysApiClientConfig> _config;
	private readonly ILogger<IPDisplaysApiClient> _logger;

	public IpDisplaysApiClientFactory(IOptions<IpDisplaysApiClientConfig> config, ILogger<IPDisplaysApiClient> logger)
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
