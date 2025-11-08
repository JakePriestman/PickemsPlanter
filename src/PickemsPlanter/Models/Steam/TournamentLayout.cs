using System.Text.Json.Serialization;

namespace PickemsPlanter.Models.Steam
{
	public class TournamentLayout
	{
		[JsonPropertyName("event")]
		public int Event { get; set; }
		[JsonPropertyName("name")]
		public string Name { get; set; }
		[JsonPropertyName("sections")]
		public List<Section> Sections { get; set; }
		[JsonPropertyName("teams")]
		public IEnumerable<Team> Teams { get; set; }

	}
}
