using System.Text.Json.Serialization;

namespace PickemsPlanter.Models.Steam
{
	public class Group
	{
		public int GroupId { get; init; }
		public string Name { get; init; }

		[JsonPropertyName("points_per_pick")]
		public int PointsPerPick { get; init; }

		[JsonPropertyName("picks_allowed")]
		public bool PicksAllowed { get; init; }
		public IEnumerable<TeamId> Teams { get; init; }
		public IEnumerable<Pick> Picks { get; init; }
	}
}
