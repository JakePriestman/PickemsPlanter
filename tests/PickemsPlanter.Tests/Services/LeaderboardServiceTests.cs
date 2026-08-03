using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PickemsPlanter.APIs;
using PickemsPlanter.Models.CoinProgress;
using PickemsPlanter.Models.Steam;
using Xunit;

namespace PickemsPlanter.Services;

public class LeaderboardServiceTests
{
	private readonly ISteamAPI _steamAPI = Substitute.For<ISteamAPI>();
	private readonly IUserEventsTableService _userEventsTableService = Substitute.For<IUserEventsTableService>();
	private readonly ICoinProgressService _coinProgressService = Substitute.For<ICoinProgressService>();
	private readonly LeaderboardService _service;

	private const string ViewerSteamId = "76500000000000001";
	private const string EventId = "25";

	public LeaderboardServiceTests()
	{
		_service = new(_steamAPI, _userEventsTableService, _coinProgressService);
	}

	private static FriendsListResult FriendsList(params string[] steamIds) => new()
	{
		FriendsList = new() { Friends = [.. steamIds.Select(id => new SteamFriend { SteamId = id })] }
	};

	private static CoinProgressResult Progress(int completed, CoinTier tier = CoinTier.Bronze) => new()
	{
		Tier = tier,
		CompletedChallenges = completed,
		TotalChallenges = 11,
		Challenges = []
	};

	[Fact]
	public async Task GetFriendsLeaderboardAsync_ReturnsFriendsListIsPrivate_WhenSteamReturnsNull()
	{
		_steamAPI.GetFriendListAsync(ViewerSteamId).Returns((FriendsListResult?)null);

		var result = await _service.GetFriendsLeaderboardAsync(ViewerSteamId, EventId);

		Assert.True(result.FriendsListIsPrivate);
		Assert.Empty(result.Entries);
	}

	[Fact]
	public async Task GetFriendsLeaderboardAsync_ExcludesFriends_WhoHaveNeverUsedTheAppForThisEvent()
	{
		_steamAPI.GetFriendListAsync(ViewerSteamId).Returns(FriendsList("76500000000000002", "76500000000000003"));
		_userEventsTableService.ExistsAsync(ViewerSteamId, EventId).Returns(false);
		_userEventsTableService.ExistsAsync("76500000000000002", EventId).Returns(true);
		_userEventsTableService.ExistsAsync("76500000000000003", EventId).Returns(false);

		_steamAPI.GetPlayerSummariesAsync(Arg.Any<IEnumerable<string>>())
			.Returns(new GetResponse<PlayerList> { Response = new() { Players = [] } });
		_coinProgressService.GetCoinProgressAsync("76500000000000002", EventId).Returns(Progress(5));

		var result = await _service.GetFriendsLeaderboardAsync(ViewerSteamId, EventId);

		Assert.False(result.FriendsListIsPrivate);
		Assert.Single(result.Entries);
		Assert.Equal("76500000000000002", result.Entries.Single().SteamId);
	}

	[Fact]
	public async Task GetFriendsLeaderboardAsync_IncludesTheViewer_WhenTheyHaveThemselvesUsedTheApp()
	{
		_steamAPI.GetFriendListAsync(ViewerSteamId).Returns(FriendsList());
		_userEventsTableService.ExistsAsync(ViewerSteamId, EventId).Returns(true);
		_steamAPI.GetPlayerSummariesAsync(Arg.Any<IEnumerable<string>>())
			.Returns(new GetResponse<PlayerList> { Response = new() { Players = [] } });
		_coinProgressService.GetCoinProgressAsync(ViewerSteamId, EventId).Returns(Progress(3));

		var result = await _service.GetFriendsLeaderboardAsync(ViewerSteamId, EventId);

		Assert.Single(result.Entries);
		Assert.Equal(ViewerSteamId, result.Entries.Single().SteamId);
	}

	[Fact]
	public async Task GetFriendsLeaderboardAsync_ReturnsNoEntries_WhenNoOneHasUsedTheAppForThisEvent()
	{
		_steamAPI.GetFriendListAsync(ViewerSteamId).Returns(FriendsList("76500000000000002"));
		_userEventsTableService.ExistsAsync(Arg.Any<string>(), EventId).Returns(false);

		var result = await _service.GetFriendsLeaderboardAsync(ViewerSteamId, EventId);

		Assert.False(result.FriendsListIsPrivate);
		Assert.Empty(result.Entries);
		await _steamAPI.DidNotReceive().GetPlayerSummariesAsync(Arg.Any<IEnumerable<string>>());
	}

	[Fact]
	public async Task GetFriendsLeaderboardAsync_OrdersEntriesByCompletedChallengesDescending()
	{
		_steamAPI.GetFriendListAsync(ViewerSteamId).Returns(FriendsList("76500000000000002", "76500000000000003"));
		_userEventsTableService.ExistsAsync(Arg.Any<string>(), EventId).Returns(true);

		_steamAPI.GetPlayerSummariesAsync(Arg.Any<IEnumerable<string>>())
			.Returns(new GetResponse<PlayerList> { Response = new() { Players = [] } });

		_coinProgressService.GetCoinProgressAsync(ViewerSteamId, EventId).Returns(Progress(4));
		_coinProgressService.GetCoinProgressAsync("76500000000000002", EventId).Returns(Progress(9, CoinTier.Gold));
		_coinProgressService.GetCoinProgressAsync("76500000000000003", EventId).Returns(Progress(1));

		var result = await _service.GetFriendsLeaderboardAsync(ViewerSteamId, EventId);

		Assert.Equal(
			["76500000000000002", ViewerSteamId, "76500000000000003"],
			result.Entries.Select(e => e.SteamId));
	}

	[Fact]
	public async Task GetFriendsLeaderboardAsync_SkipsAParticipant_WhoseCoinProgressLookupThrows_ButStillReturnsTheOthers()
	{
		// A stale/revoked auth code for one friend must not take the whole leaderboard down.
		_steamAPI.GetFriendListAsync(ViewerSteamId).Returns(FriendsList("76500000000000002"));
		_userEventsTableService.ExistsAsync(Arg.Any<string>(), EventId).Returns(true);
		_steamAPI.GetPlayerSummariesAsync(Arg.Any<IEnumerable<string>>())
			.Returns(new GetResponse<PlayerList> { Response = new() { Players = [] } });

		_coinProgressService.GetCoinProgressAsync(ViewerSteamId, EventId).Returns(Progress(6));
		_coinProgressService.GetCoinProgressAsync("76500000000000002", EventId)
			.ThrowsAsync(new Exception("Auth Code for the user 76500000000000002 for event 25 is missing in the table storage."));

		var result = await _service.GetFriendsLeaderboardAsync(ViewerSteamId, EventId);

		Assert.Single(result.Entries);
		Assert.Equal(ViewerSteamId, result.Entries.Single().SteamId);
	}

	[Fact]
	public async Task GetFriendsLeaderboardAsync_FillsInPersonaNameAndAvatar_FromTheBatchedPlayerSummaries()
	{
		_steamAPI.GetFriendListAsync(ViewerSteamId).Returns(FriendsList());
		_userEventsTableService.ExistsAsync(ViewerSteamId, EventId).Returns(true);
		_coinProgressService.GetCoinProgressAsync(ViewerSteamId, EventId).Returns(Progress(2));

		_steamAPI.GetPlayerSummariesAsync(Arg.Any<IEnumerable<string>>()).Returns(new GetResponse<PlayerList>
		{
			Response = new()
			{
				Players =
				[
					new()
					{
						SteamId = ViewerSteamId,
						PersonaName = "Jake",
						AvatarFull = "https://example.com/avatar.jpg",
						ProfileUrl = "url",
						Avatar = "a",
						AvatarMedium = "am",
						AvatarHash = "hash",
						PrimaryClanId = "0"
					}
				]
			}
		});

		var result = await _service.GetFriendsLeaderboardAsync(ViewerSteamId, EventId);

		var entry = result.Entries.Single();
		Assert.Equal("Jake", entry.PersonaName);
		Assert.Equal("https://example.com/avatar.jpg", entry.Avatar);
	}
}
