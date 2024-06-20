using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mtd.Kiosk.LEDUpdater.IPDisplaysAPI;
using Mtd.Kiosk.LEDUpdater.SanityAPI;
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
				.Configure<IpDisplaysApiClientConfig>(context.Configuration.GetSection(IpDisplaysApiClientConfig.ConfigSectionName))
				.AddOptionsWithValidateOnStart<IpDisplaysApiClientConfig>(IpDisplaysApiClientConfig.ConfigSectionName)
				.Bind(context.Configuration.GetSection(IpDisplaysApiClientConfig.ConfigSectionName));

			_ = services
				.Configure<SanityClientConfig>(context.Configuration.GetSection(SanityClientConfig.ConfigSectionName))
				.AddOptionsWithValidateOnStart<SanityClientConfig>(SanityClientConfig.ConfigSectionName)
				.Bind(context.Configuration.GetSection(SanityClientConfig.ConfigSectionName));

			_ = services.AddScoped<IpDisplaysApiClient>();
			_ = services.AddScoped<SanityClient>();

			_ = services
				.Configure<HostOptions>(hostOptions =>
				{
					hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
				});

			_ = services.AddHostedService<LEDUpdaterService>();

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

