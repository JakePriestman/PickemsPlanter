using Microsoft.Extensions.Options;
using NSubstitute;
using PickemsPlanter.Models.Configurations;
using PickemsPlanter.Models.Steam;
using System.Net;
using System.Text.Json;
using System.Web;
using Xunit;

namespace PickemsPlanter.APIs;

public class SteamAPITests
{
	private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

	private static (SteamAPI api, FakeHttpMessageHandler handler) MakeApi(Func<HttpRequestMessage, HttpResponseMessage> respond)
	{
		var handler = new FakeHttpMessageHandler(respond);
		var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.steampowered.com") };

		var config = Substitute.For<IOptionsMonitor<SteamConfig>>();
		config.CurrentValue.Returns(new SteamConfig { WebApiKey = "test-web-api-key" });

		return (new SteamAPI(httpClient, SerializerOptions, config), handler);
	}

	private static HttpResponseMessage JsonResponse(string json) =>
		new(HttpStatusCode.OK) { Content = new StringContent(json) };

	[Fact]
	public async Task GetPlayerSummeries_SendsTheApiKeyAndSteamId_AndDeserializesTheResponse()
	{
		string json = """{"response":{"players":[{"steamId":"76500000000000001","communityVisibilityState":3,"profileState":1,"personaName":"Jake","profileUrl":"url","avatar":"a","avatarMedium":"am","avatarFull":"af","avatarHash":"hash","lastLogOff":0,"personaState":1,"primaryClanId":"0","timeCreated":0,"personaStatFlags":0}]}}""";
		var (api, handler) = MakeApi(_ => JsonResponse(json));

		var result = await api.GetPlayerSummeries("76500000000000001");

		Assert.Contains("key=test-web-api-key", handler.LastRequest!.RequestUri!.Query);
		Assert.Contains("steamids=76500000000000001", handler.LastRequest.RequestUri.Query);
		Assert.Equal("Jake", result.Response.Players.Single().PersonaName);
	}

	[Fact]
	public async Task GetTournamentItemsAsync_SendsEventSteamIdAndAuthCode()
	{
		string json = """{"result":{"items":[]}}""";
		var (api, handler) = MakeApi(_ => JsonResponse(json));

		await api.GetTournamentItemsAsync("76500000000000001", "25", "auth-code-abc");

		string query = handler.LastRequest!.RequestUri!.Query;
		Assert.Contains("event=25", query);
		Assert.Contains("steamid=76500000000000001", query);
		Assert.Contains("steamidkey=auth-code-abc", query);
	}

	[Fact]
	public async Task GetTournamentLayoutAsync_DeserializesSectionsAndTeams()
	{
		string json = """{"result":{"event":25,"name":"Test Major","sections":[],"teams":[]}}""";
		var (api, _) = MakeApi(_ => JsonResponse(json));

		var result = await api.GetTournamentLayoutAsync("25");

		Assert.Equal("Test Major", result.Result.Name);
	}

	[Fact]
	public async Task GetTournamentLayoutAsync_ThrowsKeyNotFound_WhenSteamReturns404()
	{
		var (api, _) = MakeApi(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

		await Assert.ThrowsAsync<KeyNotFoundException>(() => api.GetTournamentLayoutAsync("missing-event"));
	}

	[Fact]
	public async Task GetTournamentLayoutAsync_ThrowsOnOtherFailureStatusCodes()
	{
		var (api, _) = MakeApi(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

		await Assert.ThrowsAsync<HttpRequestException>(() => api.GetTournamentLayoutAsync("25"));
	}

	[Fact]
	public async Task GetUserPredictionsAsync_SendsEventSteamIdAndAuthCode_AndDeserializesPicks()
	{
		string json = """{"result":{"picks":[{"groupId":1,"index":0,"pick":42}]}}""";
		var (api, handler) = MakeApi(_ => JsonResponse(json));

		var result = await api.GetUserPredictionsAsync("76500000000000001", "25", "auth-code-abc");

		string query = handler.LastRequest!.RequestUri!.Query;
		Assert.Contains("event=25", query);
		Assert.Contains("steamid=76500000000000001", query);
		Assert.Contains("steamidkey=auth-code-abc", query);
		Assert.Equal(42, result.Result.Picks.Single().Pick);
	}

	private static List<Team> TwoTeams() =>
	[
		new() { Logo = "teamA", TeamId = 1, ItemId = 100 },
		new() { Logo = "teamB", TeamId = 2, ItemId = 200 }
	];

	[Fact]
	public async Task PostUserPredictionsAsync_EncodesEveryPick_WithItsOwnTeamAndItemId()
	{
		var (api, handler) = MakeApi(_ => new HttpResponseMessage(HttpStatusCode.OK));

		await api.PostUserPredictionsAsync(["teamA", "teamB"], TwoTeams(), sectionId: 5, groupId: 9, "76500000000000001", "25", "auth-code-abc");

		var form = HttpUtility.ParseQueryString(handler.LastRequestBody!);
		Assert.Equal("25", form["event"]);
		Assert.Equal("76500000000000001", form["steamId"]);
		Assert.Equal("auth-code-abc", form["steamIdKey"]);
		Assert.Equal("1", form["pickId"]);
		Assert.Equal("100", form["itemId"]);
		Assert.Equal("2", form["pickId1"]);
		Assert.Equal("200", form["itemId1"]);
	}

	[Fact]
	public async Task PostUserPredictionsAsync_ThrowsOnFailureStatusCode()
	{
		var (api, _) = MakeApi(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));

		await Assert.ThrowsAsync<HttpRequestException>(() =>
			api.PostUserPredictionsAsync(["teamA"], TwoTeams(), sectionId: 5, groupId: 9, "76500000000000001", "25", "auth-code-abc"));
	}

	private static List<Section> ThreePlayoffSections() =>
	[
		new() { SectionId = 1, Name = "Quarter-Finals", Groups = [new() { GroupId = 10, Name = "QF1" }, new() { GroupId = 11, Name = "QF2" }, new() { GroupId = 12, Name = "QF3" }, new() { GroupId = 13, Name = "QF4" }] },
		new() { SectionId = 2, Name = "Semi-Finals", Groups = [new() { GroupId = 20, Name = "SF1" }, new() { GroupId = 21, Name = "SF2" }] },
		new() { SectionId = 3, Name = "Final", Groups = [new() { GroupId = 30, Name = "GF" }] }
	];

	private static List<Team> EightPlayoffTeams() =>
		[.. Enumerable.Range(1, 8).Select(i => new Team { Logo = $"team{i}", TeamId = i, ItemId = (ulong)(1000 + i) })];

	[Fact]
	public async Task PostPlayoffPredictionsAsync_EncodesQuarterFinalsSemiFinalsAndTheGrandFinal()
	{
		var (api, handler) = MakeApi(_ => new HttpResponseMessage(HttpStatusCode.OK));

		// Bracket order per SteamAPI.HandleQuarters/HandleSemis/HandleFinal: picks[0] is the
		// champion, picks[1-2] the semi-final winners, picks[3-6] the quarter-final winners.
		List<string> picks = ["team1", "team2", "team3", "team4", "team5", "team6", "team7"];

		await api.PostPlayoffPredictionsAsync(picks, EightPlayoffTeams(), ThreePlayoffSections(), "76500000000000001", "25", "auth-code-abc");

		var form = HttpUtility.ParseQueryString(handler.LastRequestBody!);
		Assert.Equal("25", form["event"]);
		Assert.Equal("76500000000000001", form["steamid"]);
		Assert.Equal("auth-code-abc", form["steamidkey"]);

		// Quarter-final 1 (picks[3] = team4) lands at the unsuffixed key.
		Assert.Equal("10", form["groupId"]);
		Assert.Equal("4", form["pickId"]);
		// Quarter-final 2 (picks[4] = team5) lands at the "1" suffix.
		Assert.Equal("11", form["groupId1"]);
		Assert.Equal("5", form["pickId1"]);

		// Semi-final 1 (picks[1] = team2) lands at the "4" suffix (i + 4, per HandleSemis).
		Assert.Equal("20", form["groupId4"]);
		Assert.Equal("2", form["pickId4"]);

		// Grand final winner (picks[0] = team1) always lands at the "6" suffix.
		Assert.Equal("30", form["groupid6"]);
		Assert.Equal("1", form["pickid6"]);
	}
}
