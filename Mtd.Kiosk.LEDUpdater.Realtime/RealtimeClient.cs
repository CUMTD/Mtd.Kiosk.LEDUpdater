using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mtd.Kiosk.LEDUpdater.Realtime.Entitites;

namespace Mtd.Kiosk.LEDUpdater.Realtime;

public class RealtimeClient
{
	public readonly RealtimeClientConfig _config;
	private readonly ILogger<RealtimeClient> _logger;
	private readonly HttpClient _httpClient;

	public RealtimeClient(ILogger<RealtimeClient> logger, HttpClient httpClient, IOptions<RealtimeClientConfig> options)
	{
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));
		ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
		ArgumentNullException.ThrowIfNull(options, nameof(options));

		_logger = logger;
		_httpClient = httpClient;
		_config = options.Value;
	}
	/// <summary>
	/// Gets the active General Messages from the Kiosk API.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns> A collection of GeneralMessage objects.
	/// </returns>
	public async Task<IReadOnlyCollection<GeneralMessage>> GetActiveMessagesAsync(CancellationToken cancellationToken)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, $"{_config.GeneralMessagingUrl}");

		var deserializedMessages = new List<GeneralMessage>();

		HttpResponseMessage? response;
		try
		{
			request.Headers.Add("X-ApiKey", _config.XApiKey);                       // TODO: authenticate at the interface level with Polly
			response = await _httpClient.SendAsync(request, cancellationToken);
			response.EnsureSuccessStatusCode();
			var responseContent = await response.Content.ReadAsStringAsync();

			_logger.LogInformation("Response for general messages: {responseContent}", responseContent);

			if (string.IsNullOrWhiteSpace(responseContent))
			{
				_logger.LogWarning("Empty response for general messages.");
				return deserializedMessages;
			}

			try
			{
				deserializedMessages = JsonSerializer.Deserialize<List<GeneralMessage>>(responseContent) ?? new List<GeneralMessage>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to deserialize general messages");
				throw new Exception("Failed to deserialize general messages", ex);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch general messages");
			throw new Exception("Failed to fetch general messages", ex);
		}
		return deserializedMessages;
	}
	/// <summary>
	/// Gets the departures for a given stop ID from the Kiosk API.
	/// </summary>
	/// <param name="stopId"></param>
	/// <param name="cancellationToken"></param>
	/// <returns>A list of Departure objects.</returns>
	public async Task<IReadOnlyCollection<Departure>> GetDeparturesForStopIdAsync(string stopId, CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(stopId, nameof(stopId));
		var request = new HttpRequestMessage(HttpMethod.Get, $"{_config.DeparturesUrl}/{stopId}");

		var deserializedDepartures = new List<Departure>();
		HttpResponseMessage? response;
		try
		{
			request.Headers.Add("X-ApiKey", _config.XApiKey);                       // TODO: authenticate at the interface level w/ polly
			response = await _httpClient.SendAsync(request, cancellationToken);
			response.EnsureSuccessStatusCode();
			var responseContent = await response.Content.ReadAsStringAsync();

			if (string.IsNullOrWhiteSpace(responseContent))
			{
				_logger.LogWarning("Empty response for stop {stopId}.", stopId);
				return deserializedDepartures;
			}

			try
			{

				deserializedDepartures = JsonSerializer.Deserialize<List<Departure>>(responseContent) ?? new List<Departure>();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to deserialize departures for stop {stopId}.", stopId);
				throw new Exception("Failed to deserialize departures for stop {stopId}.", ex);
			}

		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch deserializedDepartures for stop {stopId}.", stopId);
			throw new Exception("Failed to fetch deserializedDepartures for stop {stopId}.", ex);
		}
		return deserializedDepartures;
	}
}
