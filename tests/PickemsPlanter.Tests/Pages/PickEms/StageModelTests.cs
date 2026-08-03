using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Services;
using System.Security.Claims;
using Xunit;

namespace PickemsPlanter.Pages.PickEms;

public class StageModelTests
{
	private readonly IPickemsService _pickemsService = Substitute.For<IPickemsService>();
	private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
	private readonly IUserPredictionsCachingService _cachingService = Substitute.For<IUserPredictionsCachingService>();

	private const string AuthenticatedSteamId = "76500000000000001";
	private const string EventId = "25";

	private StageModel Model(string? steamIdClaim = AuthenticatedSteamId)
	{
		var httpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity(steamIdClaim is null
				? []
				: [new Claim(ClaimTypes.NameIdentifier, steamIdClaim)]))
		};
		_httpContextAccessor.HttpContext.Returns(httpContext);

		return new(_pickemsService, _httpContextAccessor, _cachingService)
		{
			EventId = EventId,
			EventName = "Test Event",
			Stage = Stages.Stage1
		};
	}

	// Regression coverage for #151: SteamId must come from the authenticated cookie claim,
	// never from a client-supplied route/query value, or any logged-in user could view or
	// overwrite another user's picks by editing the URL.
	[Fact]
	public void SteamId_IsDerivedFromTheAuthenticatedClaim_NotFromAnyBoundRouteValue()
	{
		var model = Model(steamIdClaim: "76500000000000099");

		Assert.Equal("76500000000000099", model.SteamId);
	}

	[Fact]
	public async Task OnGetPicks_UsesTheAuthenticatedUsersSteamId()
	{
		var model = Model();

		await model.OnGetPicks();

		await _pickemsService.Received(1).GetStagePicksAsync(Stages.Stage1, AuthenticatedSteamId, EventId);
	}

	[Fact]
	public async Task OnGetPicksAllowed_ReturnsTheServiceValue()
	{
		_pickemsService.GetStagePicksAllowedAsync(Stages.Stage1, EventId).Returns(true);
		var model = Model();

		JsonResult result = await model.OnGetPicksAllowed();

		Assert.Equal(true, result.Value);
	}

	[Fact]
	public async Task OnGetTeamNames_ReturnsTheTeamNameMapForTheEvent()
	{
		Dictionary<string, string> map = new() { ["team1"] = "Team One" };
		_pickemsService.GetTeamNameMapAsync(EventId).Returns(map);
		var model = Model();

		JsonResult result = await model.OnGetTeamNames();

		Assert.Same(map, result.Value);
	}

	[Fact]
	public async Task OnGetImages_ReturnsTheTeamsInTheStage()
	{
		List<string> teams = ["team1", "team2"];
		_pickemsService.GetTeamsInStageAsync(Stages.Stage1, EventId).Returns(teams);
		var model = Model();

		JsonResult result = await model.OnGetImages();

		Assert.Same(teams, result.Value);
	}

	[Fact]
	public async Task OnGetResults_ReturnsTheStageResults()
	{
		List<string> results = ["team1", "team2"];
		_pickemsService.GetStageResultsAsync(Stages.Stage1, EventId).Returns(results);
		var model = Model();

		JsonResult result = await model.OnGetResults();

		Assert.Same(results, result.Value);
	}

	[Fact]
	public async Task OnPostSendPicks_LooksUpTheAuthCode_ForTheAuthenticatedUsersSteamId_NotAnyOtherId()
	{
		var model = Model();
		_cachingService.GetAuthCodeFromCacheAsync(EventId, AuthenticatedSteamId).Returns("auth-code-123");

		await model.OnPostSendPicks("picks-payload");

		await _cachingService.Received(1).GetAuthCodeFromCacheAsync(EventId, AuthenticatedSteamId);
		await _pickemsService.Received(1).PostStagePickemsAsync(Stages.Stage1, "picks-payload", AuthenticatedSteamId, EventId, "auth-code-123");
	}

	[Fact]
	public async Task OnPostSendPicks_RedirectsWithoutLeakingSteamIdAsARouteValue()
	{
		var model = Model();
		_cachingService.GetAuthCodeFromCacheAsync(EventId, AuthenticatedSteamId).Returns("auth-code-123");

		var result = await model.OnPostSendPicks("picks-payload");

		var redirect = Assert.IsType<RedirectToPageResult>(result);
		Assert.DoesNotContain("SteamId", redirect.RouteValues!.Keys);
		Assert.Equal(EventId, redirect.RouteValues["EventId"]);
		Assert.Equal(Stages.Stage1, redirect.RouteValues["stage"]);
	}
}
