using Microsoft.Extensions.Logging;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.Simulator;

namespace PickemsPlanter.Services;

public interface IAdvancingSeedAutomationService
{
	Task ApplyAdvancingSeedsAsync(string eventId, Stages stage, IReadOnlyCollection<SimulatorMatchResult> completedMatches);
}

// Per Valve's Major Supplemental Rulebook, a Stage 2/3 bracket's 16 teams are always seeds
// 1-8 (pre-qualified invites, entered manually — see issue #147) plus seeds 9-16 (the
// previous stage's advancing 8, ordered by that stage's own record/Buchholz/seed tiebreak).
// This service derives the second half automatically once a stage fully resolves, and
// auto-applies it (no confirmation step — see issue #138) so seeding for the next stage
// never has to be typed in by hand.
//
// Takes the mapped completed-match list as a parameter rather than depending on
// IPandaScoreResultsService, since that service depends on IPandaScoreResultsCachingService —
// the same singleton that drives this automation — and injecting it here would create a
// circular DI dependency.
public class AdvancingSeedAutomationService(
	ISeedsTableService seedsTableService,
	ITournamentCachingService tournamentCachingService,
	ISwissStandingsCalculator standingsCalculator,
	ILogger<AdvancingSeedAutomationService> logger) : IAdvancingSeedAutomationService
{
	public async Task ApplyAdvancingSeedsAsync(string eventId, Stages stage, IReadOnlyCollection<SimulatorMatchResult> completedMatches)
	{
		Stages? nextStage = stage switch
		{
			Stages.Stage1 => Stages.Stage2,
			Stages.Stage2 => Stages.Stage3,
			_ => null
		};

		if (nextStage is null || completedMatches.Count == 0)
			return;

		var currentSeeds = await seedsTableService.GetSeedsInStageAsync(stage, eventId);

		// This stage's own seeding (the initial-seed tiebreak input) isn't fully entered
		// yet — nothing reliable to compute standings against.
		if (currentSeeds.Count != 16)
			return;

		var teams = await tournamentCachingService.GetTournamentTeamsAsync(eventId);

		Dictionary<string, int> initialSeedByLogo = [];

		foreach (var seed in currentSeeds)
		{
			var team = teams.FirstOrDefault(t => t.Name!.Equals(seed.RowKey, StringComparison.CurrentCultureIgnoreCase));

			// Can't map every seeded team to a logo — bail rather than compute a partial/incorrect standing.
			if (team?.Logo is null)
				return;

			initialSeedByLogo[$"{team.Logo}.png"] = seed.Rank;
		}

		if (!standingsCalculator.TryCalculateFinalStandings(completedMatches, initialSeedByLogo, out var standings))
			return;

		var advancing = standings.Where(s => s.Wins == 3).ToList();

		if (advancing.Count != 8)
			return;

		Dictionary<string, int> seedsByTeamName = [];

		for (int i = 0; i < advancing.Count; i++)
		{
			var team = teams.FirstOrDefault(t => $"{t.Logo}.png" == advancing[i].Team);

			if (team?.Name is null)
				return;

			seedsByTeamName[team.Name] = 9 + i;
		}

		await seedsTableService.UpsertSeedsAsync(nextStage.Value, eventId, seedsByTeamName);

		logger.LogInformation("Auto-applied advancing seeds for event {EventId}: {Stage} -> {NextStage}", eventId, stage, nextStage);
	}
}
