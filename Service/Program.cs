using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mtd.Kiosk.LedUpdater.IpDisplaysApi;
using Mtd.Kiosk.LedUpdater.Realtime;
using Mtd.Kiosk.LedUpdater.SanityClient;
using Mtd.Kiosk.LedUpdater.Service;
using Mtd.Kiosk.LedUpdater.Service.Extensions;
using Serilog;

var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName()?.Name ?? "Kiosk API";
var builder = WebApplication.CreateBuilder(args);

// Register the UnobservedTaskException handler
TaskScheduler.UnobservedTaskException += (sender, e) =>
{
	Log.Error(e.Exception, "Unobserved task exception");
	e.SetObserved(); // Mark exception as handled
};

try
{
	var host = Host
		// create default builder and add user secrets
		.CreateDefaultBuilder(args)
		.UseDefaultServiceProvider((context, options) => options.ValidateOnBuild = true)
		.ConfigureServices((context, services) =>
		{

			_ = services
				.Configure<IpDisplaysApiClientConfig>(context.Configuration.GetSection(IpDisplaysApiClientConfig.CONFIG_SECTION_NAME))
				.AddOptionsWithValidateOnStart<IpDisplaysApiClientConfig>(IpDisplaysApiClientConfig.CONFIG_SECTION_NAME)
				.Bind(context.Configuration.GetSection(IpDisplaysApiClientConfig.CONFIG_SECTION_NAME));

			_ = services
				.Configure<SanityClientConfig>(context.Configuration.GetSection(SanityClientConfig.CONFIG_SECTION_NAME))
				.AddOptionsWithValidateOnStart<SanityClientConfig>(SanityClientConfig.CONFIG_SECTION_NAME)
				.Bind(context.Configuration.GetSection(SanityClientConfig.CONFIG_SECTION_NAME));

			_ = services
				.Configure<RealtimeClientConfig>(context.Configuration.GetSection(RealtimeClientConfig.CONFIG_SECTION_NAME))
				.AddOptionsWithValidateOnStart<RealtimeClientConfig>(RealtimeClientConfig.CONFIG_SECTION_NAME)
				.Bind(context.Configuration.GetSection(RealtimeClientConfig.CONFIG_SECTION_NAME));

			_ = services.Configure<LedUpdaterServiceConfig>(context.Configuration.GetSection(LedUpdaterServiceConfig.CONFIG_SECTION_NAME))
				.AddOptionsWithValidateOnStart<LedUpdaterServiceConfig>(LedUpdaterServiceConfig.CONFIG_SECTION_NAME)
				.Bind(context.Configuration.GetSection(LedUpdaterServiceConfig.CONFIG_SECTION_NAME));

			_ = services.AddScoped<IpDisplaysApiClientFactory>();
			_ = services.AddScoped<SanityClient>();
			_ = services.AddScoped<RealtimeClient>();
			_ = services.AddScoped<LedUpdaterService>();

			_ = services
				.Configure<HostOptions>(hostOptions => hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost);

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
	Log.Fatal(ex, "{assemblyName} terminated unexpectadly.", assemblyName);
	Environment.ExitCode = 1;
}
finally
{
	Log.Information("{assemblyName} has stopped.", assemblyName);
	Log.CloseAndFlush();
}

