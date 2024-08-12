using System.Collections.Immutable;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mtd.Kiosk.LEDUpdater.SanityApi.Schema;

namespace Mtd.Kiosk.LEDUpdater.SanityApi;

// https://tuo0zvfd.api.sanity.io/v2024-03-21/data/query/production?query=*%5B+_type+%3D%3D+%22kiosk%22+%26%26+ledIp+%21%3D+null+%26%26+length%28ledIp%29+%3E%3D+7+%5D%7B%0A++_id%2C+stopId%2C+displayName%2C+ledIp%0A%7D

public class SanityClient
{
	private readonly ILogger<SanityClient> _logger;
	private readonly SanityClientConfig _config;
	private readonly HttpClient _client;

	#region Constructors

	public SanityClient(HttpClient client, ILogger<SanityClient> logger, IOptions<SanityClientConfig> config)
	{
		ArgumentNullException.ThrowIfNull(client, nameof(client));
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));
		ArgumentNullException.ThrowIfNull(config?.Value, nameof(config));

		_client = client;
		_config = config.Value;
		_logger = logger;
	}

	#endregion Constructors
	#region Methods

	private string GetQueryEndpointAddress() => $"https://{_config.ProjectId}.api.sanity.io/{_config.ApiVersion}/data/query/{_config.Dataset}?query=";

	public async Task<IReadOnlyCollection<KioskDocument>> GetKiosks(CancellationToken cancellationToken)
	{
		// TODO: remove isDevelopment before deployment
		const string query = "*[ _type == \"kiosk\" ] [ isDevelopmentKiosk && defined(ledIp) ]{ _id, stopId, displayName, ledIp, isDevelopmentKiosk}";
		var url = $"{GetQueryEndpointAddress()}{HttpUtility.UrlEncode(query)}";

		HttpResponseMessage? responseMessage = null;
		try
		{
			responseMessage = await _client.GetAsync(url, cancellationToken);
			responseMessage.EnsureSuccessStatusCode();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch from Sanity.");
		}

		if (responseMessage == null)
		{
			throw new Exception("Response message was null.");
		}

		SanityApiResponse<KioskDocument> sanityResponse;

		try
		{
			using var responseStream = await responseMessage!.Content.ReadAsStreamAsync(cancellationToken) ?? throw new Exception("Failed to read sanity response stream.");
			sanityResponse = await JsonSerializer.DeserializeAsync<SanityApiResponse<KioskDocument>>(responseStream, cancellationToken: cancellationToken) ?? throw new Exception("Failed to deserialize sanity response.");
			return sanityResponse.Result.ToImmutableArray();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to deserialize from sanity.");
			throw;
		}
	}
	#endregion Methods
}
