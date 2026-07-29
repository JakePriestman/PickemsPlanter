using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PickemsPlanter.Models.Admin;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.StorageAccount;
using PickemsPlanter.Services;
using TournamentEvent = PickemsPlanter.Models.Event.Event;

namespace PickemsPlanter.Pages.Admin;

// First authorization-gated page in the app — restricted to the site owner (AdminOnly policy,
// see Extensions/ServiceCollectionExtensions.AddAuth). Lets the admin seed a stage from a
// browser-saved copy of HLTV's Valve Ranking page (see Services/HltvRankingParser.cs for why
// this is a file upload rather than a server-side fetch).
//
// Upload always previews the parsed/matched result and requires an explicit Apply — this is a
// deliberate, manual, per-tournament action, not a background job, and a wrong match should be
// visible before it's written rather than silently applied.
[Authorize(Policy = "AdminOnly")]
public class SeedingModel(
	IEventTableService eventTableService,
	ISeedsTableService seedsTableService,
	IStageRosterService stageRosterService,
	IHltvRankingParser hltvRankingParser) : PageModel
{
	[BindProperty(SupportsGet = true)]
	public string? EventId { get; set; }

	[BindProperty(SupportsGet = true)]
	public Stages? Stage { get; set; }

	[BindProperty(SupportsGet = true)]
	public bool Applied { get; set; }

	[BindProperty]
	public List<SeedPreviewRow> PreviewRows { get; set; } = [];

	public IReadOnlyCollection<TournamentEvent> Events { get; private set; } = [];
	public IReadOnlyCollection<Seed> CurrentSeeds { get; private set; } = [];
	public string? StatusMessage { get; private set; }
	public bool StatusIsSuccess { get; private set; }
	public bool HasPreview { get; private set; }
	public bool CanApply { get; private set; }

	// Stage 1's roster is written in full (all 16); Stage 2/3's roster mixes 8 invite teams
	// (seeded here) with 8 stage-advancers (seed owned by AdvancingSeedAutomationService), so
	// only the checked subset is written.
	public bool IsInviteOnlyStage => Stage is Stages.Stage2 or Stages.Stage3;

	public async Task OnGetAsync()
	{
		await LoadEventsAndSeedsAsync();

		if (Applied)
		{
			StatusMessage = "Seeds applied.";
			StatusIsSuccess = true;
		}
	}

	public async Task<IActionResult> OnPostUploadAsync(IFormFile? file)
	{
		await LoadEventsAndSeedsAsync();

		if (EventId is null || Stage is null)
		{
			StatusMessage = "Choose an event and stage first.";
			return Page();
		}

		if (file is null || file.Length == 0)
		{
			StatusMessage = "Choose a saved HLTV ranking .html file to upload.";
			return Page();
		}

		var hltvTeams = hltvRankingParser.Parse(file.OpenReadStream());

		if (hltvTeams.Count == 0)
		{
			StatusMessage = "Couldn't find any ranked teams in that file — make sure it's a saved copy of HLTV's Valve Ranking page.";
			return Page();
		}

		var roster = await stageRosterService.GetStageRosterAsync(EventId, Stage.Value);

		if (roster.Count != 16)
		{
			StatusMessage = $"This stage's roster isn't fully known yet (found {roster.Count} of 16 teams) — nothing to match against.";
			return Page();
		}

		HashSet<int>? suggestedInvitePickIds = null;

		if (IsInviteOnlyStage)
		{
			var likelyInvites = await stageRosterService.GetLikelyInviteTeamsAsync(EventId, Stage.Value);
			suggestedInvitePickIds = likelyInvites?.Select(t => t.PickId).ToHashSet();
		}

		List<string> hltvNames = [.. hltvTeams.Select(t => t.TeamName)];

		PreviewRows = [.. roster
			.Select(team =>
			{
				string? matchedName = PandaScoreMatchMapper.ResolveTeamName(hltvNames, team.Name!);
				var hltvEntry = matchedName is null ? null : hltvTeams.FirstOrDefault(t => t.TeamName == matchedName);

				return new SeedPreviewRow
				{
					TeamName = team.Name!,
					Rank = hltvEntry?.GlobalRank,
					Matched = hltvEntry is not null,
					Selected = !IsInviteOnlyStage || (suggestedInvitePickIds?.Contains(team.PickId) ?? false)
				};
			})
			.OrderBy(r => r.Rank ?? int.MaxValue)];

		HasPreview = true;
		CanApply = ValidateSelection(out _);

		if (!CanApply)
			StatusMessage = BuildValidationMessage();

		return Page();
	}

	public async Task<IActionResult> OnPostApplyAsync()
	{
		await LoadEventsAndSeedsAsync();

		if (EventId is null || Stage is null)
		{
			StatusMessage = "Choose an event and stage first.";
			return Page();
		}

		HasPreview = true;

		if (!ValidateSelection(out var relevantRows))
		{
			CanApply = false;
			StatusMessage = BuildValidationMessage();
			return Page();
		}

		// Re-index by relative HLTV order within the written set — a global HLTV position
		// (eg. "#47") isn't a bracket seed, but the Nth-lowest among exactly the teams being
		// written is.
		var ordered = relevantRows.OrderBy(r => r.Rank).ToList();

		Dictionary<string, int> teamNameToRank = [];

		for (int i = 0; i < ordered.Count; i++)
			teamNameToRank[ordered[i].TeamName] = i + 1;

		await seedsTableService.UpsertSeedsAsync(Stage.Value, EventId, teamNameToRank);

		return RedirectToPage(new { EventId, Stage, Applied = true });
	}

	private bool ValidateSelection(out List<SeedPreviewRow> relevantRows)
	{
		relevantRows = IsInviteOnlyStage ? [.. PreviewRows.Where(r => r.Selected)] : PreviewRows;

		int expectedCount = IsInviteOnlyStage ? 8 : 16;

		return relevantRows.Count == expectedCount && relevantRows.All(r => r.Matched && r.Rank is not null);
	}

	private string BuildValidationMessage()
	{
		int expectedCount = IsInviteOnlyStage ? 8 : 16;
		var relevantRows = IsInviteOnlyStage ? PreviewRows.Where(r => r.Selected).ToList() : PreviewRows;

		if (relevantRows.Count != expectedCount)
			return $"Select exactly {expectedCount} teams before applying (currently {relevantRows.Count}).";

		var unmatched = relevantRows.Where(r => !r.Matched || r.Rank is null).Select(r => r.TeamName).ToList();

		return $"These teams didn't match the uploaded ranking: {string.Join(", ", unmatched)}. Nothing was written.";
	}

	private async Task LoadEventsAndSeedsAsync()
	{
		var events = await eventTableService.GetAllEventsAsync();

		Events = [.. events.Where(e => !e.Disabled)];

		if (EventId is not null && Stage is not null)
			CurrentSeeds = await seedsTableService.GetSeedsInStageAsync(Stage.Value, EventId);
	}
}
