using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mtd.Kiosk.IpDisplaysApi.Models;
using Mtd.Kiosk.LedUpdater.Realtime.Entitites;
using System.Collections.Immutable;
using System.Text.Json;

namespace Mtd.Kiosk.LedUpdater.Realtime;

public class RealtimeClient
{
	public readonly RealtimeClientConfig _config;
	private readonly ILogger<RealtimeClient> _logger;
	private readonly HttpClient _httpClient;

	public RealtimeClient(ILogger<RealtimeClient> logger, HttpClient httpClient, IOptions<RealtimeClientConfig> options)
	{
		ArgumentNullException.ThrowIfNull(logger, nameof(logger));
		ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
		ArgumentNullException.ThrowIfNull(options.Value, nameof(options));

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

		HttpResponseMessage? response = null;
		try
		{
			request.Headers.Add("X-ApiKey", _config.XApiKey);
			response = await _httpClient.SendAsync(request, cancellationToken);

			response.EnsureSuccessStatusCode(); // throws HttpRequestException if not successful
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "General message HTTP request did not return a good status code: {code}", response?.StatusCode);
			return [];
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch general messages");
			return [];
		}

		try
		{
			var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

			if (responseStream.Length == 0)
			{
				_logger.LogWarning("Empty response for general messages.");
				return [];
			}

			var deserialized = await JsonSerializer.DeserializeAsync<IEnumerable<GeneralMessage>>(responseStream, cancellationToken: cancellationToken);
			return (deserialized ?? []).ToImmutableArray();

		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to deserialize general messages");
			return [];
		}
	}

	/// <summary>
	/// Gets the departures for a given stop ID from the Kiosk API.
	/// </summary>
	/// <param name="stopId"></param>
	/// <param name="cancellationToken"></param>
	/// <returns>A list of Departure objects.</returns>
	public async Task<IReadOnlyCollection<Departure>> GetDeparturesForStopIdAsync(string stopId, string kioskId, CancellationToken cancellationToken)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(stopId, nameof(stopId));

		var request = new HttpRequestMessage(HttpMethod.Get, $"{_config.DeparturesUrl}/{stopId}/led?kioskId={kioskId}");

		HttpResponseMessage? response = null;
		try
		{
			request.Headers.Add("X-ApiKey", _config.XApiKey);
			response = await _httpClient.SendAsync(request, cancellationToken);
			response.EnsureSuccessStatusCode();
			var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "Departures message HTTP request did not return a good status code: {code}", response?.StatusCode);
			throw new Exception($"Departures message HTTP request did not return a good status code: {response?.StatusCode}", ex);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch Departures for stop {stopId}.", stopId);
			throw new Exception($"Failed to fetch Departures for stop {stopId}.", ex);
		}

		try
		{
			var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

			if (responseStream.Length == 0)
			{
				_logger.LogWarning("Empty response for stop {stopId}.", stopId);
				return [];
			}

			var deserialized = await JsonSerializer.DeserializeAsync<IEnumerable<Departure>>(responseStream, cancellationToken: cancellationToken);
			_logger.LogDebug("Deserialized {count} departures for stop {stopId}.", deserialized?.Count(), stopId);
			return (deserialized ?? []).ToImmutableArray();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to deserialize departures for stop {stopId}.", stopId);
			throw new Exception($"Failed to deserialize departures for stop {stopId}.", ex);
		}
	}

	public async Task<bool> GetDarkModeStatus(CancellationToken cancellationToken)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, $"{_config.DarkModeUrl}");

		HttpResponseMessage? response = null;
		try
		{
			request.Headers.Add("X-ApiKey", _config.XApiKey);
			response = await _httpClient.SendAsync(request, cancellationToken); // TODO: could cache this value?
			response.EnsureSuccessStatusCode();

			var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
			return responseContent == "true";

		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "Dark mode HTTP request did not return a good status code: {code}", response?.StatusCode);
			throw new Exception($"Dark mode HTTP request did not return a good status code: {response?.StatusCode}", ex);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to fetch dark mode status");
			throw new Exception("Failed to fetch dark mode status", ex);
		}
	}

	public async Task<bool> LogHeartbeat(string kioskId, CancellationToken cancellationToken)
	{
		var request = new HttpRequestMessage(HttpMethod.Post, $"{_config.HeartbeatUrl}/led?kioskId={kioskId}");

		HttpResponseMessage? response = null;
		try
		{
			request.Headers.Add("X-ApiKey", _config.XApiKey);
			// send in query as kioskId
			response = await _httpClient.SendAsync(request, cancellationToken);
			response.EnsureSuccessStatusCode();
			_logger.LogDebug("Logged heartbeat for {kioskId}", kioskId);
			return true;
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "Heartbeat HTTP request did not return a good status code: {code}", response?.StatusCode);
			return false;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to log heartbeat");
			return false;
		}
	}
}
