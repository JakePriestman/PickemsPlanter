using PickemsPlanter.Models.CoinProgress;

namespace PickemsPlanter.Models.Leaderboard;

public class LeaderboardEntry
{
	public required string SteamId { get; init; }

	public required string PersonaName { get; init; }

	public string? Avatar { get; init; }

	public required int CompletedChallenges { get; init; }

	public required int TotalChallenges { get; init; }

	public required CoinTier Tier { get; init; }
}
