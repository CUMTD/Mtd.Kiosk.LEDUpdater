using System.Text.Json.Serialization;

namespace Mtd.Kiosk.LedUpdater.SanityClient.Schema;
public abstract class SanityDocument
{
	[JsonPropertyName("_id")]
	public required string Id { get; set; }
}
