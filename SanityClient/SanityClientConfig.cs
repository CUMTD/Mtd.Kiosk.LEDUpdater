using System.ComponentModel.DataAnnotations;

namespace Mtd.Kiosk.LedUpdater.SanityClient;

public class SanityClientConfig
{
	public const string ConfigSectionName = "SanityApi";

	[Required]
	public required string ProjectId { get; set; }

	[Required]
	public required string Dataset { get; set; }

	[Required]
	public required string Token { get; set; }

	[Required]
	public required bool UseCdn { get; set; }

	[Required]
	public required string ApiVersion { get; set; }
}
