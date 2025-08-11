using CS2Pickems.Models.Configurations;
using CS2Pickems.Models.Steam;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CS2Pickems.APIs
{
	public interface ISteamAPI
	{
		Task<GetResponse<PlayerList>> GetPlayerSummeries(string steamId);
		Task<GetResult<TournamentItems>> GetTournamentItemsAsync(string steamId, string eventId);
		Task<GetResult<TournamentLayout>> GetTournamentLayoutAsync(string eventId);
		Task<GetResult<UserPredictions>> GetUserPredictionsAsync(string steamId, string eventId);
		Task PostUserPredictionsAsync(List<string> pickNames, List<Team> teams, int sectionId, int groupId, string steamId, string eventId);
	}

	public class SteamAPI(HttpClient httpClient, JsonSerializerOptions serializerOptions, IOptionsMonitor<SteamConfig> config) : ISteamAPI
	{
		private readonly HttpClient _httpClient = httpClient;
		private readonly JsonSerializerOptions _serializerOptions = serializerOptions;
		private readonly SteamConfig _config = config.CurrentValue;
		private const string AUTH_CODE = "8BME-SJ6H8-TAZN";

		public async Task<GetResponse<PlayerList>> GetPlayerSummeries(string steamId)
		{
			HttpRequestMessage request = new(HttpMethod.Get, $"/ISteamUser/GetPlayerSummaries/v2/?key={_config.WebApiKey}&steamids={steamId}");

			var response = await _httpClient.SendAsync(request);

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<GetResponse<PlayerList>>(json, _serializerOptions)!;
		}
		public async Task<GetResult<TournamentItems>> GetTournamentItemsAsync(string steamId, string eventId)
		{
			HttpRequestMessage request = new(HttpMethod.Get, $"/ICSGOTournaments_730/GetTournamentItems/v1?key={_config.WebApiKey}&event={eventId}&steamid={steamId}&steamidkey={AUTH_CODE}");

			var response = await _httpClient.SendAsync(request);

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<GetResult<TournamentItems>>(json, _serializerOptions)!;
		}

		public async Task<GetResult<TournamentLayout>> GetTournamentLayoutAsync(string eventId)
		{
			HttpRequestMessage request = new(HttpMethod.Get, $"/ICSGOTournaments_730/GetTournamentLayout/v1?key={_config.WebApiKey}&event={eventId}");

			var response = await _httpClient.SendAsync(request);

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<GetResult<TournamentLayout>>(json, _serializerOptions)!;
		}

		public async Task<GetResult<UserPredictions>> GetUserPredictionsAsync(string steamId, string eventId)
		{
			HttpRequestMessage request = new(HttpMethod.Get, $"/ICSGOTournaments_730/GetTournamentPredictions/v1?key={_config.WebApiKey}&event={eventId}&steamid={steamId}&steamidkey={AUTH_CODE}");

			var response = await _httpClient.SendAsync(request);

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<GetResult<UserPredictions>>(json, _serializerOptions)!;
		}

		public async Task PostUserPredictionsAsync(List<string> pickNames, List<Team> teams, int sectionId, int groupId, string steamId, string eventId)
		{
			HttpRequestMessage request = new(HttpMethod.Post, $"/ICSGOTournaments_730/UploadTournamentPredictions/v1?key={_config.WebApiKey}");

			var first = teams.First(x => x.Logo == pickNames.First());


			Dictionary<string, string> formData = new()
			{
				{"event", eventId },
				{"steamId", steamId },
				{"steamIdKey", AUTH_CODE },
				{"sectionId",$"{sectionId}" },
				{"groupId", $"{groupId}" },
				{"index", "0" },
				{"pickId", first.TeamId.ToString() },
				{"itemId", first.ItemId.ToString() }
			};

			for (int i = 1; i < pickNames.Count; i++)
			{
				var item = teams.First(x => x.Logo == pickNames[i]);

				formData.Add($"sectionId{i}", $"{sectionId}");
				formData.Add($"groupId{i}", $"{groupId}");
				formData.Add($"index{i}", i.ToString());
				formData.Add($"pickId{i}", item.TeamId.ToString());
				formData.Add($"itemId{i}", item.ItemId.ToString());
			}

			request.Content = new FormUrlEncodedContent(formData);

			var response = await _httpClient.SendAsync(request);

			var content = await response.Content.ReadAsStringAsync();
		}
	}
}

