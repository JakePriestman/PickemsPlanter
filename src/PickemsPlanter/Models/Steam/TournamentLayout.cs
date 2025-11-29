using System.Text.Json.Serialization;

namespace PickemsPlanter.Models.Steam
{
	public class TournamentLayout
	{
		[JsonPropertyName("event")]
		public int Event { get; set; }
		[JsonPropertyName("name")]
		public required string Name { get; set; }
		[JsonPropertyName("sections")]
		public required List<Section> Sections { get; set; }
		[JsonPropertyName("teams")]
		public required IReadOnlyCollection<Team> Teams { get; set; }

	}
}
