using PickemsPlanter.Models;
using PickemsPlanter.Models.Configurations;
using PickemsPlanter.Models.Steam;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PickemsPlanter.APIs
{
	public interface ISteamAPI
	{
		Task<GetResponse<PlayerList>> GetPlayerSummeries(string steamId);
		Task<GetResult<TournamentItems>> GetTournamentItemsAsync(string steamId, string eventId, string authCode);
		Task<GetResult<TournamentLayout>> GetTournamentLayoutAsync(string eventId);
		Task<GetResult<UserPredictions>> GetUserPredictionsAsync(string steamId, string eventId, string authCode);
		Task PostUserPredictionsAsync(List<string> pickNames, List<Team> teams, int sectionId, int groupId, string steamId, string eventId, string authCode);
		Task PostPlayoffPredictionsAsync(List<string> pickNames, List<Team> teams, IReadOnlyCollection<Section> playoffs, string steamId, string eventId, string authCode);
	}

	public class SteamAPI(HttpClient httpClient, JsonSerializerOptions serializerOptions, IOptionsMonitor<SteamConfig> config) : ISteamAPI
	{
		private readonly HttpClient _httpClient = httpClient;
		private readonly JsonSerializerOptions _serializerOptions = serializerOptions;
		private readonly SteamConfig _config = config.CurrentValue;

		public async Task<GetResponse<PlayerList>> GetPlayerSummeries(string steamId)
		{
			HttpRequestMessage request = new(HttpMethod.Get, $"/ISteamUser/GetPlayerSummaries/v2/?key={_config.WebApiKey}&steamids={steamId}");

			var response = await _httpClient.SendAsync(request);

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<GetResponse<PlayerList>>(json, _serializerOptions)!;
		}
		public async Task<GetResult<TournamentItems>> GetTournamentItemsAsync(string steamId, string eventId, string authCode)
		{
			HttpRequestMessage request = new(HttpMethod.Get, $"/ICSGOTournaments_730/GetTournamentItems/v1?key={_config.WebApiKey}&event={eventId}&steamid={steamId}&steamidkey={authCode}");

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

		public async Task<GetResult<UserPredictions>> GetUserPredictionsAsync(string steamId, string eventId, string authCode)
		{
			HttpRequestMessage request = new(HttpMethod.Get, $"/ICSGOTournaments_730/GetTournamentPredictions/v1?key={_config.WebApiKey}&event={eventId}&steamid={steamId}&steamidkey={authCode}");

			var response = await _httpClient.SendAsync(request);

			var json = await response.Content.ReadAsStringAsync();

			return JsonSerializer.Deserialize<GetResult<UserPredictions>>(json, _serializerOptions)!;
		}

		public async Task PostUserPredictionsAsync(List<string> pickNames, List<Team> teams, int sectionId, int groupId, string steamId, string eventId, string authCode)
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

			var response = await _httpClient.SendAsync(request);

			response.EnsureSuccessStatusCode();
		}

		public async Task PostPlayoffPredictionsAsync(List<string> pickNames, List<Team> teams, IReadOnlyCollection<Section> playoffs, string steamId, string eventId, string authCode)
		{
			HttpRequestMessage request = new(HttpMethod.Post, $"/ICSGOTournaments_730/UploadTournamentPredictions/v1?key={_config.WebApiKey}");

			Section final = playoffs.ElementAt(2);

			Dictionary<string, string> formData = HandleFinal(pickNames, teams, final, steamId, eventId, authCode);

			Section semiFinals = playoffs.ElementAt(1);

			formData = HandleSemis(formData, pickNames, teams, semiFinals);

			Section quarterFinals = playoffs.First();

			formData = HandleQuarters(formData, pickNames, teams, quarterFinals);

			request.Content = new FormUrlEncodedContent(formData);

			await _httpClient.SendAsync(request);
		}

		private static Dictionary<string, string> HandleQuarters(Dictionary<string, string> formData, List<string> pickNames, List<Team> teams, Section quarterFinals)
		{
			for (int i = 0; i < quarterFinals.Groups.Count(); i++)
			{
				int j = i + 3;
				var item = teams.First(x => x.Logo == pickNames[j]);

				var group = quarterFinals.Groups.ElementAt(i);

				formData.Add($"sectionId{j}", $"{quarterFinals.SectionId}");
				formData.Add($"groupId{j}", $"{group.GroupId}");
				formData.Add($"index{j}", j.ToString());
				formData.Add($"pickId{j}", item.TeamId.ToString());
				formData.Add($"itemId{j}", item.ItemId.ToString());
			}

			return formData;
		}

		private static Dictionary<string, string> HandleSemis(Dictionary<string, string> formData, List<string> pickNames, List<Team> teams, Section semiFinals)
		{
			for (int i = 0; i < semiFinals.Groups.Count(); i++)
			{
				int j = i + 1;
				var item = teams.First(x => x.Logo == pickNames[j]);

				var group = semiFinals.Groups.ElementAt(i);

				formData.Add($"sectionId{j}", $"{semiFinals.SectionId}");
				formData.Add($"groupId{j}", $"{group.GroupId}");
				formData.Add($"index{j}", j.ToString());
				formData.Add($"pickId{j}", item.TeamId.ToString());
				formData.Add($"itemId{j}", item.ItemId.ToString());
			}

			return formData;
		}

		private static Dictionary<string, string> HandleFinal(List<string> pickNames, List<Team> teams, Section final, string steamId, string eventId, string authCode)
		{
			var finalTeam = teams.First(x => x.Logo == pickNames.First());

			Dictionary<string, string> formData = new()
			{
				{"event", eventId },
				{"steamId", steamId },
				{"steamIdKey", authCode },
				{"sectionId",$"{final.SectionId}" },
				{"groupId", $"{final.Groups.First().GroupId}" },
				{"index", "0" },
				{"pickId", finalTeam.TeamId.ToString() },
				{"itemId", finalTeam.ItemId.ToString() }
			};

			return formData;
		}
	}
}

