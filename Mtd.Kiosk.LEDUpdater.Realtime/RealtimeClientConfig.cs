using System.ComponentModel.DataAnnotations;

namespace Mtd.Kiosk.LedUpdater.Realtime;

public class RealtimeClientConfig
{
	public const string ConfigSectionName = "RealtimeClientConfig";

	[Required, Url]
	public required string DeparturesUrl { get; set; }

	[Required, Url]
	public required string GeneralMessagingUrl { get; set; }

	[Required]
	public required string XApiKey { get; set; }
}
