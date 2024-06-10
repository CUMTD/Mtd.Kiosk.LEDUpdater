using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mtd.Kiosk.LEDUpdater.IPDisplays.API;
using ServiceReference1;

namespace Mtd.Kiosk.LEDUpdaterService.Service;
internal class LEDUpdaterService : BackgroundService, IDisposable
{
    private readonly ILogger<LEDUpdaterService> _logger;
    // private readonly IpDisplaysApi _soapClient;

    public LEDUpdaterService(ILogger<LEDUpdaterService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IpDisplaysApi _soapClient = new IpDisplaysApi("10.128.17.35", _logger);
        var client = _soapClient.GetClient();
        var request = new GetLayoutsRequest();

        var response = await client.GetLayoutsAsync(request);

        var layouts = response.layoutInfoXml;

        Console.WriteLine(layouts);

        return;


    }
    /*
   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
       _logger.LogInformation("LED Updater Service is starting.");
       _soapClient.Open();


       await _soapClient.GetLayoutsAsync(new GetLayoutsRequest()).ContinueWith((response) =>
       {
           if (response.IsCompletedSuccessfully)
           {
               _logger.LogInformation("Successfully retrieved layouts.");
               var layouts = response.Result.layoutInfoXml;
               _logger.LogInformation(layouts);
           }
           else
           {
               _logger.LogError("Failed to retrieve layouts.");
           }
       });

       stoppingToken.Register(() => _logger.LogInformation("LED Updater Service is stopping."));

       // return Task.CompletedTask;
   }*/



}
