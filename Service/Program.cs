using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mtd.Kiosk.IpDisplaysApi;
using Mtd.Kiosk.LedUpdater.Realtime;
using Mtd.Kiosk.LedUpdater.SanityClient;
using Mtd.Kiosk.LedUpdater.Service;
using Mtd.Kiosk.LedUpdater.Service.Extensions;
using Serilog;

const string LED_PREFIX = "LED_";

var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName()?.Name ?? "Kiosk API";

var logConfiguration = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.AddEnvironmentVariables(LED_PREFIX)
	.AddUserSecrets<Program>()
	.Build();

Log.Logger = new LoggerConfiguration()
	.ReadFrom.Configuration(logConfiguration)
	.CreateLogger();

var builder = WebApplication.CreateBuilder(args);

try
{
	Log.Information("{assemblyName} is starting.", assemblyName);

	var host = Host
		.CreateDefaultBuilder(args)
		.UseSerilog()
		.UseDefaultServiceProvider((context, options) => options.ValidateOnBuild = true)
		.ConfigureAppConfiguration(configureDelegate: (context, config) =>
		{
			_ = config
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);

			_ = config.AddEnvironmentVariables(LED_PREFIX);

			// add user secrets if we're in development
			if (context.HostingEnvironment.IsDevelopment())
			{
				_ = config.AddUserSecrets<Program>();
			}
		})
		.ConfigureLogging(loggingBuilder =>
		{
			loggingBuilder.ClearProviders(); // Clear default logging providers
			loggingBuilder.AddSerilog(); // Add Serilog
		})
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
			_ = services.AddScoped<SanityApiClient>();
			_ = services.AddScoped<RealtimeClient>();
			_ = services.AddScoped<LedDepartureUpdaterService>(); // we will launch these from LedHostedServiceManager

			_ = services.AddHttpClient();

			_ = services.AddHostedService<LedHostedServiceManager>();
			_ = services.AddHostedService<LedBrightnessService>();

			_ = services
				.Configure<HostOptions>(hostOptions => hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost);

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

