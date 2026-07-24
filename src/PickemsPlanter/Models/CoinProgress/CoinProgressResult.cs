namespace PickemsPlanter.Models.CoinProgress;

public class CoinProgressResult
{
	public required CoinTier Tier { get; init; }

	public required int CompletedChallenges { get; init; }

	public required int TotalChallenges { get; init; }

	public required IReadOnlyList<ChallengeProgress> Challenges { get; init; }
}
