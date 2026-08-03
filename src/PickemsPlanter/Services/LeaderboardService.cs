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

		// Run every candidate's existence check concurrently rather than one round-trip at a
		// time — with a large friends list, awaiting them sequentially made load time scale
		// with friend count instead of the single slowest lookup.
		bool[] existsResults = await Task.WhenAll(candidateSteamIds.Select(steamId => userEventsTableService.ExistsAsync(steamId, eventId)));

		List<string> participantSteamIds = [.. candidateSteamIds.Where((_, index) => existsResults[index])];

		if (participantSteamIds.Count == 0)
			return new FriendsLeaderboardResult { FriendsListIsPrivate = false, Entries = [] };

		// The batched player-summary lookup and every participant's coin-progress scoring are
		// all independent of each other, so they run concurrently too — the latter still keeps
		// its own per-participant fail-soft handling (a stale auth code shouldn't break
		// everyone else's leaderboard), just inside the parallel task instead of a loop body.
		Task<GetResponse<PlayerList>> playerSummariesTask = steamAPI.GetPlayerSummariesAsync(participantSteamIds);

		Task<(string SteamId, CoinProgressResult? Progress)>[] progressTasks = [.. participantSteamIds.Select(async steamId =>
		{
			try
			{
				return (steamId, (CoinProgressResult?)await coinProgressService.GetCoinProgressAsync(steamId, eventId));
			}
			catch (Exception)
			{
				return (steamId, (CoinProgressResult?)null);
			}
		})];

		var progressResults = await Task.WhenAll(progressTasks);
		GetResponse<PlayerList> playerSummaries = await playerSummariesTask;

		Dictionary<string, PlayerSummery> playerLookup = playerSummaries.Response.Players.ToDictionary(p => p.SteamId);

		List<LeaderboardEntry> entries = [];

		foreach (var (steamId, progress) in progressResults)
		{
			if (progress is null) continue;

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
