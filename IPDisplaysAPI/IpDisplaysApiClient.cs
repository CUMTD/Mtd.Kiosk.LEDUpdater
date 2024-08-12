using System.ServiceModel;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mtd.Kiosk.LEDUpdater.IpDisplaysApi.Models;

namespace Mtd.Kiosk.LEDUpdater.IpDisplaysApi;

public class IPDisplaysApiClient
{

	private readonly ILogger<IPDisplaysApiClient> _logger;
	private readonly Uri _uri;
	private readonly TimeSpan _timeout;

	private const int START_TIMER = 96;
	private const int STOP_TIMER = 94;
	private const int PAUSE_TIMER = 95;

	#region Constructors

	internal IPDisplaysApiClient(string ip, IOptions<IPDisplaysApiClientConfig> config, ILogger<IPDisplaysApiClient> logger)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(ip);
		ArgumentNullException.ThrowIfNull(config, nameof(config));
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));

		_uri = new Uri($"http://{ip}/soap1.wsdl");
		_timeout = TimeSpan.FromMilliseconds(config.Value.TimeoutMiliseconds);
		_logger = logger;
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

	/// <summary>
	/// Serializes a dictionary of data items into an XML string compatible with the ipDisplays API
	/// </summary>
	/// <param name="dataItems">A dictionary of dataItem names and their new values</param>
	/// <returns></returns>
	public string SerializeUpdateDataItemsXmlString(Dictionary<string, string> dataItems)
	{
		UpdateDataItemValuesXml XMLdataItems = new UpdateDataItemValuesXml();

		foreach (var item in dataItems)
		{
			XMLdataItems.DataItems.Add(new DataItem { Name = item.Key, Value = item.Value });
		}

		XmlSerializer serializer = new XmlSerializer(typeof(UpdateDataItemValuesXml));

		using StringWriter textWriter = new StringWriter();
		serializer.Serialize(textWriter, XMLdataItems);

		return textWriter.ToString();
	}

	#endregion Helpers

	#region Api Methods

	/// <summary>
	/// Refreshes the "Time_Since_Last_Update" data item on the sign. Failing to refresh this data item will cause the sign to time out and go blank.
	/// </summary>
	/// <returns></returns>
	public async Task<bool> RefreshTimer()
	{
		using var client = GetSoapClient();

		var stop = await client.SendCommandAsync(STOP_TIMER, "Time_Since_Last_Update");
		var set = await client.UpdateDataItemValueByNameAsync("Time_Since_Last_Update", DateTime.Now.ToString("M/d HH:mm:ss"));
		var start = await client.SendCommandAsync(START_TIMER, "Time_Since_Last_Update");

		return stop.Result == 0 && set.Result == 0 && start.Result == 0;
	}

	/// <summary>
	/// Ensures that a layout is enabled. If the layout is not enabled, it will be enabled. This approach prevents unnecessary setLayout calls, which can cause flickering on the sign.
	/// </summary>
	/// <param name="layoutName"></param>
	/// <returns></returns>
	public async Task<bool> EnsureLayoutEnabled(string layoutName)
	{
		using var client = GetSoapClient();
		try
		{
			var layout = await client.GetLayoutByNameAsync(new GetLayoutByNameRequest(layoutName, 0));

			if (layout.layoutInfoXml.Contains("enabled=\"1\""))
			{
				return true;
			}

			var result = await client.SetLayoutStateAsync(layoutName, 1);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "{name} Failed to Execute", nameof(EnsureLayoutEnabled));
		}
		return false;
	}

	/// <summary>
	/// Updates a single data item on the sign.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="value"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Updates multiple data items at once.
	/// </summary>
	/// <param name="dataItems">A dictionary of dataItem names mapped to their new values.</param>
	/// <returns></returns>
	public async Task<bool> UpdateDataItems(Dictionary<string, string> dataItems)
	{
		using var client = GetSoapClient();

		var xml = SerializeUpdateDataItemsXmlString(dataItems);

		_ = await client.UpdateDataItemValuesAsync(xml);

		return true;
	}
	#endregion Api Methods

}
