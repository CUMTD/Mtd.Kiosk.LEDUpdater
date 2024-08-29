using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mtd.Kiosk.LedUpdater.SanityClient.Schema;
using System.Collections.Immutable;
using System.Text.Json;
using System.Web;

namespace Mtd.Kiosk.LedUpdater.SanityClient;

public class SanityClient
{
	private readonly ILogger<SanityClient> _logger;
	private readonly SanityClientConfig _config;
	private readonly HttpClient _client;

	#region Constructors

	public SanityClient(HttpClient client, IOptions<SanityClientConfig> config, ILogger<SanityClient> logger)
	{
		ArgumentNullException.ThrowIfNull(client, nameof(client));
		ArgumentNullException.ThrowIfNull(config?.Value, nameof(config));
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));

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
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "Sanity returned at {code} status code: {message}.", responseMessage?.StatusCode, responseMessage?.ReasonPhrase);
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch from Sanity.");
			throw;
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
