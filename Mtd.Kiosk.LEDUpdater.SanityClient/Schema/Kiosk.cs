using Sanity.Linq.CommonTypes;

namespace Mtd.Kiosk.LEDUpdater.SanityAPI.Schema;

public class Kiosk : SanityDocument
{
	public Kiosk() : base() { }
	public string? LedIp { get; set; }
}
