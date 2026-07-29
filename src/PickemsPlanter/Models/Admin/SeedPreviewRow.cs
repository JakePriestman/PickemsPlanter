namespace PickemsPlanter.Models.Admin;

// One roster team's row in the HLTV-upload preview (Pages/Admin/Seeding.cshtml.cs). Round-tripped
// via hidden form fields between the upload and apply postbacks — Apply never re-parses the file.
public class SeedPreviewRow
{
	public required string TeamName { get; set; }

	// Null when this team couldn't be matched against the uploaded HLTV ranking.
	public int? Rank { get; set; }

	public bool Matched { get; set; }

	// Only meaningful for Stage 2/3 (which of the 16 roster teams are the invites to seed);
	// always true for Stage 1, where every roster team gets a seed.
	public bool Selected { get; set; }
}
