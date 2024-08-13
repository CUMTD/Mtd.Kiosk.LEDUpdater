using System.ComponentModel.DataAnnotations;

namespace Mtd.Kiosk.LedUpdater.Service;
public class LedUpdaterServiceConfig
{
	public const string ConfigSectionName = "LedUpdaterService";

	[Required]
	public required int SignUpdateInterval { get; set; }
}
