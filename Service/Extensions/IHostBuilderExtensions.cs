using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;

namespace Mtd.Kiosk.LEDUpdater.Service.Extensions;
internal static class IHostBuilderExtensions
{
    public static IHostBuilder AddOSSpecificService(this IHostBuilder builder)
    {
        // Add Appropriate Services Based on OS
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            builder.UseWindowsService(options =>
            {
                options.ServiceName = "Mtd.Kiosk.Annunciator";
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            builder.UseSystemd();
        }
        else
        {
            var os = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "OSX" : "Unknown OS";
            throw new PlatformNotSupportedException($"{os} is not supported");
        }
        return builder;
    }
}
