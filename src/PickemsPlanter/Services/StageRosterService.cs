using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.Steam;

namespace PickemsPlanter.Services;

public interface IStageRosterService
{
	Task<IReadOnlyCollection<Team>> GetStageRosterAsync(string eventId, Stages stage);
	Task<IReadOnlyCollection<Team>?> GetLikelyInviteTeamsAsync(string eventId, Stages stage);
}

// A Stage 2/3 bracket's 16-team roster mixes 8 pre-qualified invite teams with 8 teams
// advancing from the previous stage (seed already owned by AdvancingSeedAutomationService).
// Cross-checking a real completed tournament's data confirmed a stage's roster is always
// (teams also present in the previous stage's roster, i.e. advancers) union (teams new to
// this stage, i.e. invites) — so diffing the two stages' rosters identifies the invites.
//
// Confirmed directly against a live event's Steam tournament data: before the previous stage
// concludes, Steam reports this stage's roster as 24 — the previous stage's full 16-team
// candidate pool (any of whom could still advance) plus this stage's own 8 confirmed invites.
// It collapses to a clean 16 the moment the previous stage concludes, which is also exactly
// when AdvancingSeedAutomationService writes that stage's seeds 9-16. So the `!= 16` guard
// below isn't just a defensive fallback — it's the correct, confirmed signal for "the previous
// stage hasn't concluded yet, don't offer a suggestion."
public class StageRosterService(ITournamentCachingService tournamentCachingService) : IStageRosterService
{
	public async Task<IReadOnlyCollection<Team>> GetStageRosterAsync(string eventId, Stages stage)
	{
		var section = await tournamentCachingService.GetSectionAsync(eventId, stage);
		var teams = await tournamentCachingService.GetTournamentTeamsAsync(eventId);

		var pickIds = section.Groups.First().Teams.Select(t => t.PickId).ToHashSet();

		return [.. teams.Where(t => pickIds.Contains(t.PickId))];
	}

	public async Task<IReadOnlyCollection<Team>?> GetLikelyInviteTeamsAsync(string eventId, Stages stage)
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

		// Fewer/more than 16 means this stage's roster isn't cleanly resolved yet — nothing
		// reliable to diff against.
		if (currentRoster.Count != 16)
			return null;

		var previousRoster = await GetStageRosterAsync(eventId, previousStage.Value);

		if (previousRoster.Count != 16)
			return null;

		var previousPickIds = previousRoster.Select(t => t.PickId).ToHashSet();

		var invites = currentRoster.Where(t => !previousPickIds.Contains(t.PickId)).ToList();

		return invites.Count == 8 ? invites : null;
	}
}
