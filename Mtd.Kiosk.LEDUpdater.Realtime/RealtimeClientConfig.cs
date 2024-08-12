namespace Mtd.Kiosk.LEDUpdater.Realtime;

public class RealtimeClientConfig
{
	public const string ConfigSectionName = "RealtimeClientConfig";
	public required string DeparturesUrl { get; set; }

	public required string GeneralMessagingUrl { get; set; }

	public required string XApiKey { get; set; }
}
