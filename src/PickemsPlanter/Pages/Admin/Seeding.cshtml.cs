using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using PickemsPlanter.Models.Admin;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.Steam;
using PickemsPlanter.Models.StorageAccount;
using PickemsPlanter.Services;
using System.Security.Claims;
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
// picks an event, uploads that one file once, and gets a preview for all three stages at once.
// Each stage has its own checkbox rather than its own Apply button — the admin checks whichever
// stages they're ready to commit and hits one Save at the bottom, which writes all of them in
// one pass (all-or-nothing across the checked set: if any checked stage fails validation,
// nothing is written, so a bad match in one stage can't silently leave another half-applied).
[Authorize(Policy = "AdminOnly")]
public class SeedingModel(
	IEventTableService eventTableService,
	ISeedsTableService seedsTableService,
	IStageRosterService stageRosterService,
	IHltvRankingParser hltvRankingParser,
	IHttpContextAccessor httpContextAccessor,
	IAdvancingSeedAutomationService advancingSeedAutomationService,
	IPandaScoreResultsService pandaScoreResultsService,
	ILogger<SeedingModel> logger) : PageModel
{
	public static readonly IReadOnlyList<Stages> SeedableStages = [Stages.Stage1, Stages.Stage2, Stages.Stage3];

	public string? PersonaName => httpContextAccessor.HttpContext?.User.FindFirst("PersonaName")?.Value;

	public string? Avatar => httpContextAccessor.HttpContext?.User.FindFirst("Avatar")?.Value;

	[BindProperty(SupportsGet = true)]
	public string? EventId { get; set; }

	[BindProperty(SupportsGet = true)]
	public bool Applied { get; set; }

	[BindProperty(SupportsGet = true)]
	public List<Stages> AppliedStages { get; set; } = [];

	// Only bound on an Apply postback — one entry per stage, always all three regardless of
	// which are checked, since Apply re-validates and re-displays every stage's preview in one
	// pass (see OnPostApplyAsync).
	[BindProperty]
	public List<StageSelection> Selections { get; set; } = [];

	public IReadOnlyCollection<TournamentEvent> Events { get; private set; } = [];
	public Dictionary<Stages, IReadOnlyCollection<Seed>> CurrentSeedsByStage { get; private set; } = [];
	public Dictionary<Stages, StagePreview> Previews { get; private set; } = [];

	public string? StatusMessage { get; private set; }
	public bool StatusIsSuccess { get; private set; }
	public bool HasPreview { get; private set; }

	// Stage 1's roster is written in full (all 16); Stage 2/3's roster mixes 8 invite teams
	// (seeded here — see IStageRosterService.GetLikelyInviteTeamsAsync) with 8 stage-advancers
	// (seed owned by AdvancingSeedAutomationService), so only the confirmed 8 invites are
	// ever shown or written here.
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
			StatusMessage = AppliedStages.Count == 0
				? "Seeds applied."
				: $"{string.Join(", ", AppliedStages.Select(StageLabel))} seeds saved.";
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

		// Re-validate and re-populate every stage's preview from what was actually posted back
		// (rather than trusting the client), so a resubmit after a validation failure re-renders
		// exactly the same view the initial upload preview would have.
		foreach (var selection in Selections)
			Previews[selection.Stage] = new StagePreview(selection.Rows, ValidateRows(selection.Stage, selection.Rows, out string? message), message);

		var toApply = Selections.Where(s => s.Selected).ToList();

		if (toApply.Count == 0)
		{
			StatusMessage = "Select at least one stage to save.";
			StatusIsSuccess = false;
			return Page();
		}

		if (toApply.Any(s => !Previews[s.Stage].CanApply))
		{
			StatusMessage = "Some selected stages couldn't be saved — see details below. Nothing was written.";
			StatusIsSuccess = false;
			return Page();
		}

		foreach (var selection in toApply)
		{
			// Re-index by relative HLTV order within the written set — a global HLTV position
			// (eg. "#47") isn't a bracket seed, but the Nth-lowest among exactly the teams being
			// written is.
			var ordered = selection.Rows.OrderBy(r => r.Rank).ToList();

			Dictionary<string, int> teamNameToRank = [];

			for (int i = 0; i < ordered.Count; i++)
				teamNameToRank[ordered[i].TeamName] = i + 1;

			await seedsTableService.UpsertSeedsAsync(selection.Stage, EventId, teamNameToRank);
		}

		// A stage's own seeds are the Buchholz tiebreak input for computing the NEXT stage's
		// 9-16 (AdvancingSeedAutomationService) — without this, that recompute would only
		// happen on PandaScoreResultsCachingService's next poll tick, so the next stage's
		// advancer order would still reflect the seeds just replaced until then. Re-trigger it
		// immediately using whatever's already cached, so Save reflects the update right away.
		foreach (var selection in toApply)
		{
			try
			{
				var completedMatches = await pandaScoreResultsService.GetCompletedMatchesAsync(EventId, selection.Stage);
				await advancingSeedAutomationService.ApplyAdvancingSeedsAsync(EventId, selection.Stage, completedMatches);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Advancing-seed re-trigger after Seeding Apply failed for event {EventId} stage {Stage}", EventId, selection.Stage);
			}
		}

		return RedirectToPage(new { EventId, Applied = true, AppliedStages = toApply.Select(s => s.Stage).ToList() });
	}

	private async Task<StagePreview> BuildPreviewAsync(string eventId, Stages stage, IReadOnlyList<HltvRankedTeam> hltvTeams)
	{
		var roster = await stageRosterService.GetStageRosterAsync(eventId, stage);

		if (roster.Count < 16)
			return new([], false, $"{StageLabel(stage)}'s roster isn't fully known yet (found {roster.Count} of 16 teams) — nothing to match against.");

		IReadOnlyList<Team> teamsToSeed;

		if (IsInviteOnlyStage(stage))
		{
			// Always exactly the 8 confirmed invites — never the previous stage's advancers,
			// and never the "candidate" teams Steam still lists before that stage concludes
			// (see StageRosterService: the confirmed 8 are knowable either way, so there's
			// nothing ambiguous left to show a full 16-team pick from).
			var confirmedInvites = await stageRosterService.GetLikelyInviteTeamsAsync(eventId, stage);

			if (confirmedInvites is null)
				return new([], false, $"{StageLabel(stage)}'s 8 confirmed invite teams couldn't be determined yet (Steam is showing {roster.Count} candidate teams for this stage).");

			teamsToSeed = confirmedInvites;
		}
		else
		{
			// Stage 1 has no "previous stage" to inflate its roster — more than 16 here is
			// an unexpected/anomalous state, not a normal pre-resolution one.
			if (roster.Count > 16)
				return new([], false, $"{StageLabel(stage)}'s bracket isn't finalized yet — Steam is still showing {roster.Count} candidate teams.");

			teamsToSeed = roster;
		}

		var rows = BuildPreviewRows(teamsToSeed, hltvTeams);
		bool canApply = ValidateRows(stage, rows, out string? message);

		return new(rows, canApply, message);
	}

	private static List<SeedPreviewRow> BuildPreviewRows(IEnumerable<Team> teams, IReadOnlyList<HltvRankedTeam> hltvTeams)
	{
		List<string> hltvNames = [.. hltvTeams.Select(t => t.TeamName)];

		return [.. teams
			.Select(team =>
			{
				string? matchedName = PandaScoreMatchMapper.ResolveTeamName(hltvNames, team.Name!);
				var hltvEntry = matchedName is null ? null : hltvTeams.FirstOrDefault(t => t.TeamName == matchedName);

				return new SeedPreviewRow
				{
					TeamName = team.Name!,
					Rank = hltvEntry?.GlobalRank,
					Matched = hltvEntry is not null
				};
			})
			.OrderBy(r => r.Rank ?? int.MaxValue)];
	}

	private static bool ValidateRows(Stages stage, List<SeedPreviewRow> rows, out string? message)
	{
		int expectedCount = IsInviteOnlyStage(stage) ? 8 : 16;

		if (rows.Count != expectedCount)
		{
			message = $"Expected {expectedCount} teams for {StageLabel(stage)}, found {rows.Count}.";
			return false;
		}

		var unmatched = rows.Where(r => !r.Matched || r.Rank is null).Select(r => r.TeamName).ToList();

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

// One stage's checked state + round-tripped preview rows on an Apply postback (see
// SeedingModel.Selections) — always carries the stage explicitly rather than relying on list
// order, so the Apply handler doesn't depend on the form rendering stages in a fixed sequence.
public class StageSelection
{
	public Stages Stage { get; set; }
	public bool Selected { get; set; }
	public List<SeedPreviewRow> Rows { get; set; } = [];
}
