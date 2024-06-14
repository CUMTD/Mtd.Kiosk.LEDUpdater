using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mtd.Kiosk.LEDUpdater.IPDisplaysAPI;
using Mtd.Kiosk.LEDUpdater.Service;
using Mtd.Kiosk.LEDUpdater.Service.Extensions;
using Serilog;

try
{
	var host = Host
		.CreateDefaultBuilder(args)
		.ConfigureServices((context, services) =>
		{
			_ = services
				.Configure<IpDisplaysApiClientConfig>(context.Configuration.GetSection(IpDisplaysApiClientConfig.ConfigSectionName))
				.AddOptionsWithValidateOnStart<IpDisplaysApiClientConfig>(IpDisplaysApiClientConfig.ConfigSectionName)
				.Bind(context.Configuration.GetSection(IpDisplaysApiClientConfig.ConfigSectionName));

			_ = services.AddScoped<IpDisplaysApiClient>();

			_ = services
				.Configure<HostOptions>(hostOptions =>
				{
					hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
				});

			_ = services.AddHostedService<LEDUpdaterService>();

		})

		.AddOSSpecificService()
		.Build();

	await host.RunAsync();
}
catch (Exception ex)
{
	Log.Fatal(ex, "Host terminated unexpectedly");
	Environment.ExitCode = 1;
}
finally
{
	Log.CloseAndFlush();
}

