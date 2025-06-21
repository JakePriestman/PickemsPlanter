using CS2Pickems.Models;
using System.Text.Json;

namespace CS2Pickems.APIs
{
	public interface ISteamAPI
	{
		Task<GetResponse<TournamentItems>> GetTournamentItemsAsync();
		Task<GetResponse<TournamentLayout>> GetTournamentLayoutAsync();
		Task<GetResponse<UserPredictions>> GetUserPredictionsAsync();

		Task PostUserPredictionsAsync(List<string> pickNames, List<Team> teams, int sectionId, int groupId);
	}

	public class SteamAPI(HttpClient httpClient, JsonSerializerOptions serializerOptions) : ISteamAPI
	{
		private readonly HttpClient _httpClient = httpClient;
		private readonly JsonSerializerOptions _serializerOptions = serializerOptions;
		private const string API_KEY = "";
		private const string AUTH_CODE = "";
		private const string STEAM_ID = "";
		private const string EVENT_ID = "";

		public async Task<GetResponse<TournamentItems>> GetTournamentItemsAsync()
		{
			HttpRequestMessage request = new(HttpMethod.Get, $"/ICSGOTournaments_730/GetTournamentItems/v1?key={API_KEY}&event={EVENT_ID}&steamid={STEAM_ID}&steamidkey={AUTH_CODE}");

			var response = await _httpClient.SendAsync(request);

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<GetResponse<TournamentItems>>(json, _serializerOptions)!;
		}

		public async Task<GetResponse<TournamentLayout>> GetTournamentLayoutAsync()
		{
			HttpRequestMessage request = new(HttpMethod.Get, $"/ICSGOTournaments_730/GetTournamentLayout/v1?key={API_KEY}&event={EVENT_ID}");

			var response = await _httpClient.SendAsync(request);

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<GetResponse<TournamentLayout>>(json, _serializerOptions)!;
		}

		public async Task<GetResponse<UserPredictions>> GetUserPredictionsAsync()
		{
			HttpRequestMessage request = new(HttpMethod.Get, $"/ICSGOTournaments_730/GetTournamentPredictions/v1?key={API_KEY}&event={EVENT_ID}&steamid={STEAM_ID}&steamidkey={AUTH_CODE}");

			var response = await _httpClient.SendAsync(request);

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<GetResponse<UserPredictions>>(json, _serializerOptions)!;
		}

		public async Task PostUserPredictionsAsync(List<string> pickNames, List<Team> teams, int sectionId, int groupId)
		{
			HttpRequestMessage request = new(HttpMethod.Post, $"/ICSGOTournaments_730/UploadTournamentPredictions/v1?key={API_KEY}");

			var first = teams.First(x => x.Logo == pickNames.First());


			Dictionary<string, string> formData = new()
			{
				{"event", EVENT_ID.ToString() },
				{"steamId", STEAM_ID.ToString() },
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

