using System.ComponentModel.DataAnnotations;

namespace Mtd.Kiosk.LedUpdater.IpDisplaysApi;
public class IpDisplaysApiClientConfig
{
	public const string ConfigSectionName = "IPDisplaysApiClient";

	[Required, Range(1, int.MaxValue)]
	public required int TimeoutMiliseconds { get; set; }
}
