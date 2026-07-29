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
// This is only ever used as a default suggestion (see Pages/Admin/Seeding.cshtml.cs) since
// it's unverified whether Steam's tournament layout populates a not-yet-decided advancer
// slot before that stage actually concludes.
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
