using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.Steam;

namespace PickemsPlanter.Services;

public interface IStageRosterService
{
	Task<IReadOnlyList<Team>> GetStageRosterAsync(string eventId, Stages stage);
	Task<IReadOnlyList<Team>?> GetLikelyInviteTeamsAsync(string eventId, Stages stage);
}

// A Stage 2/3 bracket's 16-team roster mixes 8 pre-qualified invite teams with 8 teams
// advancing from the previous stage (seed already owned by AdvancingSeedAutomationService).
//
// Confirmed directly against a live event's Steam tournament data: before the previous stage
// concludes, Steam reports this stage's roster as 24 — the previous stage's full 16-team
// candidate pool (any of whom could still advance) plus this stage's own 8 confirmed invites,
// with the 8 confirmed invites always the FIRST 8 entries in Steam's own team order (this is
// the same assumption PickemsService.GetTeamsInStageAsync already relies on for the live
// Pick'Ems picker: `imageUrls.Count > 16 ? imageUrls.Take(8) : imageUrls`). Once the previous
// stage concludes, the roster collapses to a clean 16 — but Steam then reorders the list
// (confirmed against real archived tournament data: invites and advancers end up interleaved,
// not invites-first), so position can no longer be trusted and a roster-diff against the
// previous stage's resolved roster is the only reliable signal at that point.
public class StageRosterService(ITournamentCachingService tournamentCachingService) : IStageRosterService
{
	public async Task<IReadOnlyList<Team>> GetStageRosterAsync(string eventId, Stages stage)
	{
		var section = await tournamentCachingService.GetSectionAsync(eventId, stage);
		var teams = await tournamentCachingService.GetTournamentTeamsAsync(eventId);

		var teamsByPickId = teams.ToDictionary(t => t.PickId);

		// Preserves Steam's own team order (not the order GetTournamentTeamsAsync happens to
		// return teams in) — GetLikelyInviteTeamsAsync's "first 8" case depends on this.
		return [.. section.Groups.First().Teams
			.Select(t => teamsByPickId.GetValueOrDefault(t.PickId))
			.Where(t => t is not null)!];
	}

	public async Task<IReadOnlyList<Team>?> GetLikelyInviteTeamsAsync(string eventId, Stages stage)
	{
		Stages? previousStage = stage switch
		{
			Stages.Stage2 => Stages.Stage1,
			Stages.Stage3 => Stages.Stage2,
			_ => null
		};

		if (previousStage is null)
			return null;

		var currentRoster = await GetStageRosterAsync(eventId, stage);

		if (currentRoster.Count < 16)
			return null;

		// Previous stage hasn't concluded yet — Steam's own confirmed invites are always the
		// first 8 in its team order, regardless of how many candidate slots follow.
		if (currentRoster.Count > 16)
			return [.. currentRoster.Take(8)];

		var previousRoster = await GetStageRosterAsync(eventId, previousStage.Value);

		if (previousRoster.Count != 16)
			return null;

		var previousPickIds = previousRoster.Select(t => t.PickId).ToHashSet();

		var invites = currentRoster.Where(t => !previousPickIds.Contains(t.PickId)).ToList();

		return invites.Count == 8 ? invites : null;
	}
}
