using Microsoft.Extensions.Logging;
using Mtd.Kiosk.LEDUpdater.Realtime.Entitites;

namespace Mtd.Kiosk.LEDUpdater.IpDisplaysApi;
public class LedSign
{
	private readonly IPDisplaysApiClient _client;
	private readonly ILogger<LedSign> _logger;

	public LedSign(IPDisplaysApiClient client, ILogger<LedSign> logger)
	{
		ArgumentNullException.ThrowIfNull(client);
		ArgumentNullException.ThrowIfNull(logger);

		_client = client;
		_logger = logger;
	}

	public Task<bool> UpdateSign(Departure departure)
	{
		return UpdateSign(departure, null);
	}

	public async Task<bool> UpdateSign(Departure topDeparture, Departure? bottomDeparture)
	{
		await _client.RefreshTimer();

		var dataItems = new Dictionary<string, string>
		{
			{ "Top_Left", topDeparture.Route },
			{ "Top_Right", topDeparture.Time },
			{ "Bottom_Left", bottomDeparture?.Route ?? string.Empty },
			{ "Bottom_Right", bottomDeparture?.Time ?? string.Empty }
		};

		await _client.UpdateDataItems(dataItems);

		var result = await _client.EnsureLayoutEnabled("TwoLineDepartures");

		return result;
	}

	public async Task<bool> UpdateSign(string topMessage, Departure? bottomDeparture)
	{
		await _client.RefreshTimer();

		var dataItems = new Dictionary<string, string>
		{
			{ "Top_Center", topMessage },
			{ "Bottom_Left", bottomDeparture?.Route ?? string.Empty },
			{ "Bottom_Right", bottomDeparture?.Time ?? string.Empty }
		};

		await _client.UpdateDataItems(dataItems);

		var result = await _client.EnsureLayoutEnabled("OneLineMessage");

		return result;
	}

	public async Task<bool> UpdateSign(string topMessage, string bottomMessage)
	{
		await _client.RefreshTimer();


		var dataItems = new Dictionary<string, string>
		{
			{ "Top_Center", topMessage },
			{ "Bottom_Center", bottomMessage }
		};

		await _client.UpdateDataItems(dataItems);

		var result = await _client.EnsureLayoutEnabled("TwoLineMessage");

		return result;
	}

	public async Task<bool> BlankScreen()
	{
		var result = await UpdateSign(string.Empty, string.Empty);

		return result;
	}

}

