using System.Text.Json.Serialization;

namespace Mtd.Kiosk.LedUpdater.SanityClient.Schema;

public class SanityApiResponse<T> where T : SanityDocument
{
	[JsonPropertyName("query")]
	public required string Query { get; set; }

	[JsonPropertyName("result")]
	public required IEnumerable<T> Result { get; set; }

	[JsonPropertyName("syncTags")]
	public required string[] SyncTags { get; set; }

	[JsonPropertyName("ms")]
	public required int Ms { get; set; }
}
