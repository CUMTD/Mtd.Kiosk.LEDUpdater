using System.ComponentModel.DataAnnotations;

namespace Mtd.Kiosk.LedUpdater.Realtime;

public class RealtimeClientConfig
{
	public const string CONFIG_SECTION_NAME = "RealtimeClientConfig";

	[Required, Url]
	public required string DeparturesUrl { get; set; }

	[Required, Url]
	public required string GeneralMessagingUrl { get; set; }

	[Required, Url]
	public required string DarkModeUrl { get; set; }

	[Required]
	public required string XApiKey { get; set; }
}
