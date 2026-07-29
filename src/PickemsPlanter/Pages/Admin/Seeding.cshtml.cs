using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PickemsPlanter.Models.Admin;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.StorageAccount;
using PickemsPlanter.Services;
using TournamentEvent = PickemsPlanter.Models.Event.Event;

namespace PickemsPlanter.Pages.Admin;

// One stage's parsed/matched preview, built once per upload (see BuildPreviewAsync).
public record StagePreview(List<SeedPreviewRow> Rows, bool CanApply, string? Message);

// First authorization-gated page in the app — restricted to the site owner (AdminOnly policy,
// see Extensions/ServiceCollectionExtensions.AddAuth). Lets the admin seed a tournament from a
// browser-saved copy of HLTV's Valve Ranking page (see Services/HltvRankingParser.cs for why
// this is a file upload rather than a server-side fetch).
//
// The same HLTV ranking snapshot seeds Stage 1's 16 teams and Stage 2/3's 8 invite teams each
// (all three are pre-tournament data, fixed before a single match is played) — so the admin
// picks an event, uploads that one file once, and gets a preview + independent Apply action for
// all three stages at once, instead of re-uploading the same file per stage.
//
// Upload always previews the parsed/matched result and requires an explicit Apply per stage —
// this is a deliberate, manual, per-tournament action, not a background job, and a wrong match
// should be visible before it's written rather than silently applied.
[Authorize(Policy = "AdminOnly")]
public class SeedingModel(
	IEventTableService eventTableService,
	ISeedsTableService seedsTableService,
	IStageRosterService stageRosterService,
	IHltvRankingParser hltvRankingParser) : PageModel
{
	public static readonly IReadOnlyList<Stages> SeedableStages = [Stages.Stage1, Stages.Stage2, Stages.Stage3];

	[BindProperty(SupportsGet = true)]
	public string? EventId { get; set; }

	[BindProperty(SupportsGet = true)]
	public bool Applied { get; set; }

	[BindProperty(SupportsGet = true)]
	public Stages? AppliedStage { get; set; }

	// Only bound on an Apply postback — each stage's Apply form is independent, so this (and
	// PreviewRows below) only ever carries the one stage that was actually submitted.
	[BindProperty]
	public Stages Stage { get; set; }

	[BindProperty]
	public List<SeedPreviewRow> PreviewRows { get; set; } = [];

	public IReadOnlyCollection<TournamentEvent> Events { get; private set; } = [];
	public Dictionary<Stages, IReadOnlyCollection<Seed>> CurrentSeedsByStage { get; private set; } = [];
	public Dictionary<Stages, StagePreview> Previews { get; private set; } = [];

	public string? StatusMessage { get; private set; }
	public bool StatusIsSuccess { get; private set; }
	public bool HasPreview { get; private set; }

	// Stage 1's roster is written in full (all 16); Stage 2/3's roster mixes 8 invite teams
	// (seeded here) with 8 stage-advancers (seed owned by AdvancingSeedAutomationService), so
	// only the checked subset is written.
	public static bool IsInviteOnlyStage(Stages stage) => stage is Stages.Stage2 or Stages.Stage3;

	public static string StageLabel(Stages stage) => stage switch
	{
		Stages.Stage1 => "Stage 1",
		Stages.Stage2 => "Stage 2 invites",
		Stages.Stage3 => "Stage 3 invites",
		_ => stage.ToString()
	};

	public async Task OnGetAsync()
	{
		await LoadEventAndSeedsAsync();

		if (Applied)
		{
			StatusMessage = AppliedStage is null ? "Seeds applied." : $"{StageLabel(AppliedStage.Value)} seeds applied.";
			StatusIsSuccess = true;
		}
	}

	public async Task<IActionResult> OnPostUploadAsync(IFormFile? file)
	{
		await LoadEventAndSeedsAsync();

		if (EventId is null)
		{
			StatusMessage = "Choose an event first.";
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

		foreach (var stage in SeedableStages)
			Previews[stage] = await BuildPreviewAsync(EventId, stage, hltvTeams);

		HasPreview = true;

		return Page();
	}

	public async Task<IActionResult> OnPostApplyAsync()
	{
		await LoadEventAndSeedsAsync();

		if (EventId is null)
		{
			StatusMessage = "Choose an event first.";
			return Page();
		}

		HasPreview = true;

		if (!ValidateSelection(Stage, PreviewRows, out var relevantRows, out string? message))
		{
			Previews[Stage] = new StagePreview(PreviewRows, false, message);
			return Page();
		}

		// Re-index by relative HLTV order within the written set — a global HLTV position
		// (eg. "#47") isn't a bracket seed, but the Nth-lowest among exactly the teams being
		// written is.
		var ordered = relevantRows.OrderBy(r => r.Rank).ToList();

		Dictionary<string, int> teamNameToRank = [];

		for (int i = 0; i < ordered.Count; i++)
			teamNameToRank[ordered[i].TeamName] = i + 1;

		await seedsTableService.UpsertSeedsAsync(Stage, EventId, teamNameToRank);

		return RedirectToPage(new { EventId, Applied = true, AppliedStage = Stage });
	}

	private async Task<StagePreview> BuildPreviewAsync(string eventId, Stages stage, IReadOnlyList<HltvRankedTeam> hltvTeams)
	{
		var roster = await stageRosterService.GetStageRosterAsync(eventId, stage);

		if (roster.Count < 16)
			return new([], false, $"{StageLabel(stage)}'s roster isn't fully known yet (found {roster.Count} of 16 teams) — nothing to match against.");

		// Steam reports 24 for a Stage 2/3 whose previous stage hasn't concluded yet — the
		// previous stage's full 16-team candidate pool plus this stage's own 8 confirmed
		// invites — and only collapses to a clean 16 once that stage finishes. See
		// StageRosterService for the confirmed behavior this message is describing.
		if (roster.Count > 16)
			return new([], false, $"{StageLabel(stage)}'s bracket isn't finalized yet — Steam is still showing {roster.Count} candidate teams, which means the previous stage hasn't concluded. Check back once it has.");

		HashSet<int>? suggestedInvitePickIds = null;

		if (IsInviteOnlyStage(stage))
		{
			var likelyInvites = await stageRosterService.GetLikelyInviteTeamsAsync(eventId, stage);
			suggestedInvitePickIds = likelyInvites?.Select(t => t.PickId).ToHashSet();
		}

		List<string> hltvNames = [.. hltvTeams.Select(t => t.TeamName)];

		List<SeedPreviewRow> rows = [.. roster
			.Select(team =>
			{
				string? matchedName = PandaScoreMatchMapper.ResolveTeamName(hltvNames, team.Name!);
				var hltvEntry = matchedName is null ? null : hltvTeams.FirstOrDefault(t => t.TeamName == matchedName);

				return new SeedPreviewRow
				{
					TeamName = team.Name!,
					Rank = hltvEntry?.GlobalRank,
					Matched = hltvEntry is not null,
					Selected = !IsInviteOnlyStage(stage) || (suggestedInvitePickIds?.Contains(team.PickId) ?? false)
				};
			})
			.OrderBy(r => r.Rank ?? int.MaxValue)];

		bool canApply = ValidateSelection(stage, rows, out _, out string? message);

		return new(rows, canApply, message);
	}

	private static bool ValidateSelection(Stages stage, List<SeedPreviewRow> rows, out List<SeedPreviewRow> relevantRows, out string? message)
	{
		bool inviteOnly = IsInviteOnlyStage(stage);
		relevantRows = inviteOnly ? [.. rows.Where(r => r.Selected)] : rows;

		int expectedCount = inviteOnly ? 8 : 16;

		if (relevantRows.Count != expectedCount)
		{
			message = $"Select exactly {expectedCount} teams before applying (currently {relevantRows.Count}).";
			return false;
		}

		var unmatched = relevantRows.Where(r => !r.Matched || r.Rank is null).Select(r => r.TeamName).ToList();

		if (unmatched.Count > 0)
		{
			message = $"These teams didn't match the uploaded ranking: {string.Join(", ", unmatched)}. Nothing was written.";
			return false;
		}

		message = null;
		return true;
	}

	private async Task LoadEventAndSeedsAsync()
	{
		var events = await eventTableService.GetAllEventsAsync();

		Events = [.. events.Where(e => !e.Disabled)];

		if (EventId is null)
			return;

		foreach (var stage in SeedableStages)
			CurrentSeedsByStage[stage] = await seedsTableService.GetSeedsInStageAsync(stage, EventId);
	}
}
