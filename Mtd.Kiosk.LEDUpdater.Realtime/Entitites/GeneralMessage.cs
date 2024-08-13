using System.Text.Json.Serialization;

namespace Mtd.Kiosk.LedUpdater.Realtime.Entitites;
public class GeneralMessage(string stopId, string message, bool blockRealtime)
{
	[JsonPropertyName("stopId")]
	public string StopId { get; set; } = stopId;
	[JsonPropertyName("message")]
	public string Message { get; set; } = message;

	[JsonPropertyName("blockRealtime")]
	public bool BlockRealtime { get; set; } = blockRealtime;
}
