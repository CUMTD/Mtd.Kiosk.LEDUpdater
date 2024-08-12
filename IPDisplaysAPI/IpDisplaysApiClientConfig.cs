using System.ComponentModel.DataAnnotations;

namespace Mtd.Kiosk.LEDUpdater.IpDisplaysApi;
public class IPDisplaysApiClientConfig
{
	public const string ConfigSectionName = "IPDisplaysApiClient";

	[Required, Range(1, int.MaxValue)]
	public required int TimeoutMiliseconds { get; set; }
}
