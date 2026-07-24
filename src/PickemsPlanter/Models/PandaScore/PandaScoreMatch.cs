using System.Text.Json.Serialization;

namespace PickemsPlanter.Models.PandaScore;

public class PandaScoreMatch
{
	public int Id { get; init; }

	public required string Name { get; init; }

	public required string Status { get; init; }

	[JsonPropertyName("tournament_id")]
	public int TournamentId { get; init; }

	[JsonPropertyName("winner_id")]
	public int? WinnerId { get; init; }

	[JsonPropertyName("number_of_games")]
	public int NumberOfGames { get; init; }

	public IReadOnlyCollection<PandaScoreOpponent> Opponents { get; init; } = [];

	public IReadOnlyCollection<PandaScoreResult> Results { get; init; } = [];
}
