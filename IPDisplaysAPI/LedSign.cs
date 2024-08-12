using Mtd.Kiosk.LEDUpdater.Realtime.Entitites;

namespace Mtd.Kiosk.LEDUpdater.IpDisplaysApi;
public class LedSign
{
	private readonly IPDisplaysApiClient _client;

	public LedSign(IPDisplaysApiClient client)
	{
		ArgumentNullException.ThrowIfNull(client);
		_client = client;
	}

	public async Task<bool> UpdateTwoLineDepartures(Departure topDeparture, Departure? bottomDeparture)
	{
		await _client.RefreshTimer();

		var dataItems = new Dictionary<string, string>
		{
			{ "Top_Left", topDeparture.Route },
			{ "Top_Right", topDeparture.Time },
			{ "Bottom_Left", bottomDeparture?.Route ?? "" },
			{ "Bottom_Right", bottomDeparture?.Time ?? "" }
		};

		await _client.UpdateDataItems(dataItems);

		var result = await _client.EnsureLayoutEnabled("TwoLineDepartures");

		return result;

	}

	public async Task<bool> UpdateOneLineMessage(string topMessage, Departure? bottomDeparture)
	{
		await _client.RefreshTimer();

		var dataItems = new Dictionary<string, string>
		{
			{ "Top_Center", topMessage },
			{ "Bottom_Left", bottomDeparture?.Route ?? "" },
			{ "Bottom_Right", bottomDeparture?.Time ?? "" }
		};

		await _client.UpdateDataItems(dataItems);

		var result = await _client.EnsureLayoutEnabled("OneLineMessage");

		return result;
	}

	public async Task<bool> UpdateTwoLineMessage(string topMessage, string bottomMessage)
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
		var result = await UpdateTwoLineMessage("", "");

		return result;
	}

}

