using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mtd.Kiosk.LEDUpdater.IpDisplaysApi;
using Mtd.Kiosk.LEDUpdater.Realtime;
using Mtd.Kiosk.LEDUpdater.SanityApi;
using Mtd.Kiosk.LEDUpdater.Service;
using Mtd.Kiosk.LEDUpdater.Service.Extensions;
using Serilog;

try
{
	var host = Host
		// create default builder and add user secrets
		.CreateDefaultBuilder(args)
		.UseDefaultServiceProvider((context, options) =>
		{
			options.ValidateOnBuild = false;
		})
		.ConfigureServices((context, services) =>
		{
			_ = services
				.Configure<IPDisplaysApiClientConfig>(context.Configuration.GetSection(IPDisplaysApiClientConfig.ConfigSectionName))
				.AddOptionsWithValidateOnStart<IPDisplaysApiClientConfig>(IPDisplaysApiClientConfig.ConfigSectionName)
				.Bind(context.Configuration.GetSection(IPDisplaysApiClientConfig.ConfigSectionName));

			_ = services
				.Configure<SanityClientConfig>(context.Configuration.GetSection(SanityClientConfig.ConfigSectionName))
				.AddOptionsWithValidateOnStart<SanityClientConfig>(SanityClientConfig.ConfigSectionName)
				.Bind(context.Configuration.GetSection(SanityClientConfig.ConfigSectionName));

			_ = services
				.Configure<RealtimeClientConfig>(context.Configuration.GetSection(RealtimeClientConfig.ConfigSectionName))
				.AddOptionsWithValidateOnStart<RealtimeClientConfig>(RealtimeClientConfig.ConfigSectionName)
				.Bind(context.Configuration.GetSection(RealtimeClientConfig.ConfigSectionName));

			_ = services.AddScoped<IPDisplaysApiClientFactory>();
			_ = services.AddScoped<SanityClient>();
			_ = services.AddScoped<RealtimeClient>();

			_ = services
				.Configure<HostOptions>(hostOptions =>
				{
					hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
				});

			_ = services.AddHttpClient();


			_ = services.AddHostedService<LedUpdaterService>();

		})
		.AddOSSpecificService()
		.Build();
	// print out if we're in development environment
	Log.Information("Environment: {Environment}", host.Services.GetRequiredService<IHostEnvironment>().EnvironmentName);

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

