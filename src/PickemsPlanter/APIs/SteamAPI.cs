using Microsoft.Extensions.Options;
using PickemsPlanter.Models.Configurations;
using PickemsPlanter.Models.Steam;
using System.Text.Json;

namespace PickemsPlanter.APIs
{
	public interface ISteamAPI
	{
		Task<GetResponse<PlayerList>> GetPlayerSummeries(string steamId);
		Task<GetResult<TournamentItems>> GetTournamentItemsAsync(string steamId, string eventId, string authCode);
		Task<GetResult<TournamentLayout>> GetTournamentLayoutAsync(string eventId);
		Task<GetResult<UserPredictions>> GetUserPredictionsAsync(string steamId, string eventId, string authCode);
		Task PostUserPredictionsAsync(List<string> pickNames, IReadOnlyCollection<Team> teams, int sectionId, int groupId, string steamId, string eventId, string authCode);
		Task PostPlayoffPredictionsAsync(List<string> pickNames, IReadOnlyCollection<Team> teams, IReadOnlyCollection<Section> playoffs, string steamId, string eventId, string authCode);
	}

	public class SteamAPI(HttpClient httpClient, JsonSerializerOptions serializerOptions, IOptionsMonitor<SteamConfig> config) : ISteamAPI
	{
		private readonly SteamConfig _config = config.CurrentValue;

		public async Task<GetResponse<PlayerList>> GetPlayerSummeries(string steamId)
		{
			HttpRequestMessage request = new(HttpMethod.Get, $"/ISteamUser/GetPlayerSummaries/v2/?key={_config.WebApiKey}&steamids={steamId}");

			var response = await httpClient.SendAsync(request);

			response.EnsureSuccessStatusCode();

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<GetResponse<PlayerList>>(json, serializerOptions)!;
		}
		public async Task<GetResult<TournamentItems>> GetTournamentItemsAsync(string steamId, string eventId, string authCode)
		{
			HttpRequestMessage request = new(HttpMethod.Get, $"/ICSGOTournaments_730/GetTournamentItems/v1?key={_config.WebApiKey}&event={eventId}&steamid={steamId}&steamidkey={authCode}");

			var response = await httpClient.SendAsync(request);

			response.EnsureSuccessStatusCode();

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<GetResult<TournamentItems>>(json, serializerOptions)!;
		}

		public async Task<GetResult<TournamentLayout>> GetTournamentLayoutAsync(string eventId)
		{
			HttpRequestMessage request = new(HttpMethod.Get, $"/ICSGOTournaments_730/GetTournamentLayout/v1?key={_config.WebApiKey}&event={eventId}");

			var response = await httpClient.SendAsync(request);

			response.EnsureSuccessStatusCode();

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<GetResult<TournamentLayout>>(json, serializerOptions)!;
		}

		public async Task<GetResult<UserPredictions>> GetUserPredictionsAsync(string steamId, string eventId, string authCode)
		{
			HttpRequestMessage request = new(HttpMethod.Get, $"/ICSGOTournaments_730/GetTournamentPredictions/v1?key={_config.WebApiKey}&event={eventId}&steamid={steamId}&steamidkey={authCode}");

			var response = await httpClient.SendAsync(request);

			response.EnsureSuccessStatusCode();

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<GetResult<UserPredictions>>(json, serializerOptions)!;
		}

		public async Task PostUserPredictionsAsync(List<string> pickNames, IReadOnlyCollection<Team> teams, int sectionId, int groupId, string steamId, string eventId, string authCode)
		{
			HttpRequestMessage request = new(HttpMethod.Post, $"/ICSGOTournaments_730/UploadTournamentPredictions/v1?key={_config.WebApiKey}");

			var first = teams.First(x => x.Logo == pickNames.First());


			Dictionary<string, string> formData = new()
			{
				{"event", eventId },
				{"steamId", steamId },
				{"steamIdKey", authCode },
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

			var response = await httpClient.SendAsync(request);

			response.EnsureSuccessStatusCode();
		}

		public async Task PostPlayoffPredictionsAsync(List<string> pickNames, IReadOnlyCollection<Team> teams, IReadOnlyCollection<Section> playoffs, string steamId, string eventId, string authCode)
		{
			HttpRequestMessage request = new(HttpMethod.Post, $"/ICSGOTournaments_730/UploadTournamentPredictions/v1?key={_config.WebApiKey}");

			Section quarterFinals = playoffs.First();

			Dictionary<string, string> formData = HandleQuarters(pickNames, teams, quarterFinals, steamId, eventId, authCode);

			Section semiFinals = playoffs.ElementAt(1);

			formData = HandleSemis(formData, pickNames, teams, semiFinals);

			Section final = playoffs.ElementAt(2);

			formData = HandleFinal(formData, pickNames, teams, final);

			request.Content = new FormUrlEncodedContent(formData);

			var response = await httpClient.SendAsync(request);

			response.EnsureSuccessStatusCode();
		}

		private static Dictionary<string, string> HandleQuarters(List<string> pickNames, IReadOnlyCollection<Team> teams, Section quarterFinals, string steamId, string eventId, string authCode)
		{
			Dictionary<string, string> formData = [];

			formData.Add("event", eventId);
			formData.Add("steamid", steamId);
			formData.Add("steamidkey", authCode);


			for (int i = 0; i < quarterFinals.Groups.Count(); i++)
			{
				int j = i + 3;
				var item = teams.First(x => x.Logo == pickNames[j]);

				var group = quarterFinals.Groups.ElementAt(i);

				if (i == 0)
				{
					formData.Add($"sectionId", $"{quarterFinals.SectionId}");
					formData.Add($"groupId", $"{group.GroupId}");
					formData.Add($"index", "0");
					formData.Add($"pickId", item.TeamId.ToString());
					formData.Add($"itemId", item.ItemId.ToString());
				}
				else
				{
					formData.Add($"sectionId{i}", $"{quarterFinals.SectionId}");
					formData.Add($"groupId{i}", $"{group.GroupId}");
					formData.Add($"index{i}", "0");
					formData.Add($"pickId{i}", item.TeamId.ToString());
					formData.Add($"itemId{i}", item.ItemId.ToString());
				}
			}

			return formData;
		}

		private static Dictionary<string, string> HandleSemis(Dictionary<string, string> formData, List<string> pickNames, IReadOnlyCollection<Team> teams, Section semiFinals)
		{
			for (int i = 0; i < semiFinals.Groups.Count(); i++)
			{
				int j = i + 1;
				int k = i + 4;
				var item = teams.First(x => x.Logo == pickNames[j]);

				var group = semiFinals.Groups.ElementAt(i);

				formData.Add($"sectionId{k}", $"{semiFinals.SectionId}");
				formData.Add($"groupId{k}", $"{group.GroupId}");
				formData.Add($"index{k}", "0");
				formData.Add($"pickId{k}", item.TeamId.ToString());
				formData.Add($"itemId{k}", item.ItemId.ToString());
			}

			return formData;
		}

		private static Dictionary<string, string> HandleFinal(Dictionary<string, string> formData, List<string> pickNames, IReadOnlyCollection<Team> teams, Section final)
		{
			var finalTeam = teams.First(x => x.Logo == pickNames.First());

			formData.Add("sectionid6", final.SectionId.ToString());
			formData.Add("groupid6", final.Groups.First().GroupId.ToString());
			formData.Add("index6", "0");
			formData.Add("pickid6", finalTeam.TeamId.ToString());
			formData.Add("itemid6", finalTeam.ItemId.ToString());

			return formData;
		}
	}
}

