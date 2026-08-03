using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using PickemsPlanter.Models.Configurations;
using PickemsPlanter.Models.Leaderboard;
using PickemsPlanter.Services;
using System.Security.Claims;
using Xunit;

namespace PickemsPlanter.Pages.Profile;

public class OverviewModelTests
{
	private readonly IUserEventsTableService _tableStorageService = Substitute.For<IUserEventsTableService>();
	private readonly IUserPredictionsCachingService _cachingService = Substitute.For<IUserPredictionsCachingService>();
	private readonly ITournamentCachingService _tournamentCachingService = Substitute.For<ITournamentCachingService>();
	private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
	private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
	private readonly IEventTableService _eventTableService = Substitute.For<IEventTableService>();
	private readonly ICoinProgressService _coinProgressService = Substitute.For<ICoinProgressService>();
	private readonly ILeaderboardService _leaderboardService = Substitute.For<ILeaderboardService>();
	private readonly IOptionsMonitor<AdminConfig> _adminConfig = Substitute.For<IOptionsMonitor<AdminConfig>>();

	private const string AuthenticatedSteamId = "76500000000000001";
	private const string EventId = "25";

	private OverviewModel Model()
	{
		var httpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, AuthenticatedSteamId)]))
		};
		_httpContextAccessor.HttpContext.Returns(httpContext);
		_adminConfig.CurrentValue.Returns(new AdminConfig { SteamId = "someone-else" });

		return new(_tableStorageService, _cachingService, _tournamentCachingService, _memoryCache, _httpContextAccessor,
			_eventTableService, _coinProgressService, _leaderboardService, _adminConfig);
	}

	[Fact]
	public async Task OnGetLeaderboard_UsesTheAuthenticatedUsersSteamId_AsTheViewer()
	{
		var model = Model();
		_leaderboardService.GetFriendsLeaderboardAsync(AuthenticatedSteamId, EventId)
			.Returns(new FriendsLeaderboardResult { FriendsListIsPrivate = false, Entries = [] });

		await model.OnGetLeaderboard(EventId);

		await _leaderboardService.Received(1).GetFriendsLeaderboardAsync(AuthenticatedSteamId, EventId);
	}

	[Fact]
	public async Task OnGetLeaderboard_ReturnsTheServiceResultAsJson()
	{
		var model = Model();
		var leaderboard = new FriendsLeaderboardResult { FriendsListIsPrivate = true, Entries = [] };
		_leaderboardService.GetFriendsLeaderboardAsync(AuthenticatedSteamId, EventId).Returns(leaderboard);

		JsonResult result = await model.OnGetLeaderboard(EventId);

		Assert.Same(leaderboard, result.Value);
	}
}
