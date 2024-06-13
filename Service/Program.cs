using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mtd.Kiosk.LEDUpdater.Service.Extensions;
using Mtd.Kiosk.LEDUpdaterService.Service;
using Serilog;

try
{
	var host = Host
		.CreateDefaultBuilder(args)
		.ConfigureAppConfiguration((context, config) =>
		{
			var basePath = context.HostingEnvironment.ContentRootPath;
			config
				.SetBasePath(basePath)
				.AddJsonFile("appsettings.json", true, true); // TODO

			if (context.HostingEnvironment.IsDevelopment())
			{
				var assembly = Assembly.GetExecutingAssembly();
				config.AddUserSecrets(assembly, true);
			}
		})

		.ConfigureServices((context, services) =>
		{
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

