using Microsoft.Extensions.Options;
using PickemsPlanter.Models.Configurations;
using PickemsPlanter.Models.PandaScore;
using System.Text.Json;

namespace PickemsPlanter.APIs;

public interface IPandaScoreApi
{
	Task<IReadOnlyCollection<PandaScoreMatch>?> GetTournamentMatchesAsync(int tournamentId);
}

public class PandaScoreAPI(HttpClient httpClient, JsonSerializerOptions serializerOptions, IOptionsMonitor<PandaScoreConfig> config) : IPandaScoreApi
{
	private readonly PandaScoreConfig _config = config.CurrentValue;

	// Returns null when the call failed/shape was unexpected (caller should keep whatever it had cached),
	// an empty collection when PandaScore genuinely has no matches for the tournament yet.
	public async Task<IReadOnlyCollection<PandaScoreMatch>?> GetTournamentMatchesAsync(int tournamentId)
	{
		try
		{
			HttpRequestMessage request = new(HttpMethod.Get, $"/tournaments/{tournamentId}/matches?per_page=100&token={_config.ApiToken}");

			var response = await httpClient.SendAsync(request);

			if (!response.IsSuccessStatusCode)
				return null;

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<List<PandaScoreMatch>>(json, serializerOptions);
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
		{
			return null;
		}
	}
}
