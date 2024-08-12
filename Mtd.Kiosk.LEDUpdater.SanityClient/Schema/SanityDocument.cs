using System.Text.Json.Serialization;

namespace Mtd.Kiosk.LEDUpdater.SanityApi.Schema;
public abstract class SanityDocument
{
	[JsonPropertyName("_id")]
	public required string Id { get; set; }
}
