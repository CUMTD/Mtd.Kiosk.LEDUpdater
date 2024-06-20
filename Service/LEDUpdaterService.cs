using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mtd.Kiosk.LEDUpdater.IPDisplaysAPI;

namespace Mtd.Kiosk.LEDUpdater.Service;
internal class LEDUpdaterService : BackgroundService, IDisposable
{
	private readonly IpDisplaysApiClient _client;
	private readonly ILogger<LEDUpdaterService> _logger;
	private readonly SanityAPI.SanityClient _sanityClient;

	public LEDUpdaterService(IpDisplaysApiClient client, ILogger<LEDUpdaterService> logger, SanityAPI.SanityClient sanityClient)
	{
		ArgumentNullException.ThrowIfNull(client, nameof(client));
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));
		ArgumentNullException.ThrowIfNull(sanityClient, nameof(sanityClient));

		_client = client;
		_logger = logger;
		_sanityClient = sanityClient;
	}


	protected async override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var client = _client.GetSoapClient();
		var response = await client.GetLayoutsAsync(new GetLayoutsRequest());

		_logger.LogInformation("Got XML: {xml} ", response.layoutInfoXml);


		var sampleDocuments = new List<SanityAPI.Schema.Kiosk>();

		try
		{
			var sanityDataContext = _sanityClient.GetSanityDataContext();

			_logger.LogInformation("docs count: {count}", sanityDataContext.Documents.Count());


			var docs = sanityDataContext.DocumentSet<SanityAPI.Schema.Kiosk>().ToList();

			sampleDocuments.AddRange(docs);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting documents from Sanity");
		}

		// print all documents that have an Led ip
		foreach (var doc in sampleDocuments)
		{
			if (doc.LedIp != null && doc.LedIp != "")
				_logger.LogInformation("LedIp: {doc}", doc.LedIp);
		}
	}
}
