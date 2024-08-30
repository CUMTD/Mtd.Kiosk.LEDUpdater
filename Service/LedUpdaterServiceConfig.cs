using System.ComponentModel.DataAnnotations;

namespace Mtd.Kiosk.LedUpdater.Service;
public class LedUpdaterServiceConfig
{
	public const string CONFIG_SECTION_NAME = "LedUpdaterService";

	[Required]
	public required int SignUpdateInterval { get; set; }
	[Required]
	public required int BrightnessUpdateInterval { get; set; }

	[Required, Range(1, 127)]
	public required int LightModeBrightness { get; set; }

	[Required, Range(1, 127)]
	public required int DarkModeBrightness { get; set; }
}
