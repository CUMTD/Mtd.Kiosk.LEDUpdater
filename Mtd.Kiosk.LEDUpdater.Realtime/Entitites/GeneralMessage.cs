using System.Text.Json.Serialization;

namespace Mtd.Kiosk.LEDUpdater.Realtime.Entitites;
public class GeneralMessage
{
	[JsonPropertyName("stopId")]
	public string StopId { get; set; }
	[JsonPropertyName("message")]
	public string Message { get; set; }

	[JsonPropertyName("blockRealtime")]
	public bool BlockRealtime { get; set; }

	public GeneralMessage(string stopId, string message, bool blockRealtime)
	{
		StopId = stopId;
		Message = message;
		BlockRealtime = blockRealtime;
	}

}
