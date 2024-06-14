using System.ServiceModel;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mtd.Kiosk.LEDUpdater.IPDisplaysAPI;

public class IpDisplaysApiClient
{

	private readonly ILogger<IpDisplaysApiClient> _logger;
	private readonly Uri _uri;
	private readonly TimeSpan _timeout;

	#region Constructors

	public IpDisplaysApiClient(IOptions<IpDisplaysApiClientConfig> config, ILogger<IpDisplaysApiClient> logger)
	{
		ArgumentNullException.ThrowIfNull(config, nameof(config));
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));

		_logger = logger;
		_uri = new Uri($"http://{config.Value.Ip}/soap1.wsdl");
		_timeout = TimeSpan.FromMilliseconds(config.Value.TimeoutMiliseconds);
	}

	#endregion Constructors

	#region Helpers

	public SignSvrSoapPortClient GetSoapClient()
	{
		var binding = new BasicHttpBinding
		{
			MaxBufferSize = int.MaxValue,
			ReaderQuotas = XmlDictionaryReaderQuotas.Max,
			MaxReceivedMessageSize = int.MaxValue,
			AllowCookies = true,
			CloseTimeout = _timeout,
			OpenTimeout = _timeout,
			ReceiveTimeout = _timeout,
			SendTimeout = _timeout
		};
		var endpointAddress = new EndpointAddress(_uri);
		return new SignSvrSoapPortClient(binding, endpointAddress);
	}

	#endregion Helpers

	#region Api Methods

	public async Task<bool> SetLayout(string layoutName, bool enabled)
	{
		using var client = GetSoapClient();
		try
		{
			_ = await client.SetLayoutStateAsync(layoutName, enabled ? 1 : 0);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "{name} Failed to Execute", nameof(SetLayout));
		}
		return false;
	}

	public async Task<bool> UpdateDataItem(string name, string value)
	{
		using var client = GetSoapClient();
		try
		{
			_ = await client.UpdateDataItemValueByNameAsync(name, value);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "{name} Failed to Execute", nameof(UpdateDataItem));
		}
		return false;
	}

	#endregion Api Methods

}
