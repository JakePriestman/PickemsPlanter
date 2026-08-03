using PickemsPlanter.APIs;
using PickemsPlanter.Models.CoinProgress;
using PickemsPlanter.Models.Leaderboard;
using PickemsPlanter.Models.Steam;

namespace PickemsPlanter.Services;

public interface ILeaderboardService
{
	Task<FriendsLeaderboardResult> GetFriendsLeaderboardAsync(string viewerSteamId, string eventId);
}

// Friends-only by design (matches the CS2 client's own Pick'Ems leaderboard) and, unlike an
// "all app users" leaderboard, needs no new Table Storage schema: userEvents is already
// partitioned by steamId, so filtering a bounded friends list down to participants is just
// point lookups, not a cross-partition scan.
public class LeaderboardService(ISteamAPI steamAPI, IUserEventsTableService userEventsTableService, ICoinProgressService coinProgressService) : ILeaderboardService
{
	public async Task<FriendsLeaderboardResult> GetFriendsLeaderboardAsync(string viewerSteamId, string eventId)
	{
		FriendsListResult? friendsList = await steamAPI.GetFriendListAsync(viewerSteamId);

		if (friendsList is null)
			return new FriendsLeaderboardResult { FriendsListIsPrivate = true, Entries = [] };

		// The viewer isn't their own Steam friend, but belongs on their own leaderboard too.
		List<string> candidateSteamIds = [viewerSteamId, .. friendsList.FriendsList.Friends.Select(f => f.SteamId)];

		List<string> participantSteamIds = [];

		foreach (var steamId in candidateSteamIds)
		{
			if (await userEventsTableService.ExistsAsync(steamId, eventId))
				participantSteamIds.Add(steamId);
		}

		if (participantSteamIds.Count == 0)
			return new FriendsLeaderboardResult { FriendsListIsPrivate = false, Entries = [] };

		GetResponse<PlayerList> playerSummaries = await steamAPI.GetPlayerSummariesAsync(participantSteamIds);

		Dictionary<string, PlayerSummery> playerLookup = playerSummaries.Response.Players.ToDictionary(p => p.SteamId);

		List<LeaderboardEntry> entries = [];

		foreach (var steamId in participantSteamIds)
		{
			CoinProgressResult progress;

			try
			{
				progress = await coinProgressService.GetCoinProgressAsync(steamId, eventId);
			}
			catch (Exception)
			{
				// One participant's stored auth code going stale (eg. revoked in Steam since
				// they last used the app) must not take the whole leaderboard down for
				// everyone else viewing it — same fail-soft principle as PandaScoreResultsCachingService.
				continue;
			}

			playerLookup.TryGetValue(steamId, out var player);

			entries.Add(new LeaderboardEntry
			{
				SteamId = steamId,
				PersonaName = player?.PersonaName ?? steamId,
				Avatar = player?.AvatarFull,
				CompletedChallenges = progress.CompletedChallenges,
				TotalChallenges = progress.TotalChallenges,
				Tier = progress.Tier
			});
		}

		return new FriendsLeaderboardResult
		{
			FriendsListIsPrivate = false,
			Entries = [.. entries.OrderByDescending(e => e.CompletedChallenges).ThenBy(e => e.PersonaName)]
		};
	}
}
