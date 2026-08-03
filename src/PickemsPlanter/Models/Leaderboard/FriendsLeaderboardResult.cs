namespace PickemsPlanter.Models.Leaderboard;

public class FriendsLeaderboardResult
{
	// Steam's GetFriendList requires the viewer's own friends list to be set to Public;
	// if it isn't, the call returns 401 and there's no way to know who to rank at all —
	// distinct from a viewer whose friends list is public but empty/all non-participants.
	public required bool FriendsListIsPrivate { get; init; }

	public required IReadOnlyList<LeaderboardEntry> Entries { get; init; }
}
