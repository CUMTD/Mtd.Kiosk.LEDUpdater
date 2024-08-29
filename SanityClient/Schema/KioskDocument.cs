using System.Text.Json.Serialization;

namespace Mtd.Kiosk.LedUpdater.SanityClient.Schema;

public class KioskDocument : SanityDocument
{
	[JsonPropertyName("stopId")]
	public required string StopId { get; set; }

	[JsonPropertyName("displayName")]
	public string? DisplayName { get; set; }

	[JsonPropertyName("ledIp")]
	public required string LedIp { get; set; }
}
