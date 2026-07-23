using System.Text.Json.Serialization;

namespace PickemsPlanter.Models.PandaScore;

public class PandaScoreTeam
{
	public int Id { get; init; }

	public string? Name { get; init; }

	public string? Acronym { get; init; }

	[JsonPropertyName("image_url")]
	public string? ImageUrl { get; init; }
}
