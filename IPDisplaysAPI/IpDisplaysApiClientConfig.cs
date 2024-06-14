using System.ComponentModel.DataAnnotations;

namespace Mtd.Kiosk.LEDUpdater.IPDisplaysAPI;
public class IpDisplaysApiClientConfig
{
	public const string ConfigSectionName = "IpDisplaysApiClient";

	[Required, RegularExpression("^((25[0-5]|(2[0-4]|1\\d|[1-9]|)\\d)\\.?\\b){4}$")]
	public required string Ip { get; set; }

	[Required, Range(1, int.MaxValue)]
	public required int TimeoutMiliseconds { get; set; }
}
