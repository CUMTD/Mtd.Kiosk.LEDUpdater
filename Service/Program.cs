using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mtd.Kiosk.LedUpdater.IpDisplaysApi;
using Mtd.Kiosk.LedUpdater.Realtime;
using Mtd.Kiosk.LedUpdater.SanityClient;
using Mtd.Kiosk.LedUpdater.Service;
using Mtd.Kiosk.LedUpdater.Service.Extensions;
using Serilog;

try
{
	var host = Host
		// create default builder and add user secrets
		.CreateDefaultBuilder(args)
		.UseDefaultServiceProvider((context, options) =>
		{
			options.ValidateOnBuild = true;
		})
		.ConfigureServices((context, services) =>
		{

			_ = services
				.Configure<IpDisplaysApiClientConfig>(context.Configuration.GetSection(IpDisplaysApiClientConfig.CONFIG_SECTION_NAME))
				.AddOptionsWithValidateOnStart<IpDisplaysApiClientConfig>(IpDisplaysApiClientConfig.CONFIG_SECTION_NAME)
				.Bind(context.Configuration.GetSection(IpDisplaysApiClientConfig.CONFIG_SECTION_NAME));

			_ = services
				.Configure<SanityClientConfig>(context.Configuration.GetSection(SanityClientConfig.ConfigSectionName))
				.AddOptionsWithValidateOnStart<SanityClientConfig>(SanityClientConfig.ConfigSectionName)
				.Bind(context.Configuration.GetSection(SanityClientConfig.ConfigSectionName));

			_ = services
				.Configure<RealtimeClientConfig>(context.Configuration.GetSection(RealtimeClientConfig.ConfigSectionName))
				.AddOptionsWithValidateOnStart<RealtimeClientConfig>(RealtimeClientConfig.ConfigSectionName)
				.Bind(context.Configuration.GetSection(RealtimeClientConfig.ConfigSectionName));

			_ = services.Configure<LedUpdaterServiceConfig>(context.Configuration.GetSection(LedUpdaterServiceConfig.ConfigSectionName))
				.AddOptionsWithValidateOnStart<LedUpdaterServiceConfig>(LedUpdaterServiceConfig.ConfigSectionName)
				.Bind(context.Configuration.GetSection(LedUpdaterServiceConfig.ConfigSectionName));

			_ = services.AddScoped<IpDisplaysApiClientFactory>();
			_ = services.AddScoped<SanityClient>();
			_ = services.AddScoped<RealtimeClient>();
			_ = services.AddScoped<LedUpdaterService>();

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

