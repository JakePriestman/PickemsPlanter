namespace PickemsPlanter.Models.Admin;

// One team's row in the HLTV-upload preview (Pages/Admin/Seeding.cshtml.cs) — always exactly
// the set of teams that will be written for this stage (Stage 1's full 16, or Stage 2/3's
// confirmed 8 invites from IStageRosterService.GetLikelyInviteTeamsAsync). Round-tripped via
// hidden form fields between the upload and apply postbacks — Apply never re-parses the file.
public class SeedPreviewRow
{
	public required string TeamName { get; set; }

	// Null when this team couldn't be matched against the uploaded HLTV ranking.
	public int? Rank { get; set; }

	public bool Matched { get; set; }
}
