using System.Text.Json.Serialization;

namespace PickemsPlanter.Models.PandaScore;

public class PandaScoreResult
{
	[JsonPropertyName("team_id")]
	public int TeamId { get; init; }

	public int Score { get; init; }
}
