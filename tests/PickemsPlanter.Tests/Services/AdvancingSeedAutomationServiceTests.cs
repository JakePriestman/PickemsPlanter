using Microsoft.Extensions.Logging;
using NSubstitute;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.Simulator;
using PickemsPlanter.Models.Steam;
using PickemsPlanter.Models.StorageAccount;
using Xunit;

namespace PickemsPlanter.Services;

public class AdvancingSeedAutomationServiceTests
{
	private readonly AdvancingSeedAutomationService _service;
	private readonly ISeedsTableService _seedsTableService = Substitute.For<ISeedsTableService>();
	private readonly ITournamentCachingService _tournamentCachingService = Substitute.For<ITournamentCachingService>();
	private readonly ISwissStandingsCalculator _standingsCalculator = Substitute.For<ISwissStandingsCalculator>();
	private readonly ILogger<AdvancingSeedAutomationService> _logger = Substitute.For<ILogger<AdvancingSeedAutomationService>>();

	private static readonly string[] Letters = ["a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p"];

	public AdvancingSeedAutomationServiceTests()
	{
		_service = new(_seedsTableService, _tournamentCachingService, _standingsCalculator, _logger);
	}

	private static List<Team> AllTeams() =>
		[.. Letters.Select(l => new Team { Name = $"Team {l.ToUpperInvariant()}", Logo = $"team{l}" })];

	private static List<Seed> AllCurrentSeeds(string eventId) =>
		[.. Letters.Select((l, i) => new Seed { PartitionKey = eventId, RowKey = $"Team {l.ToUpperInvariant()}", Rank = i + 1 })];

	private static List<SimulatorMatchResult> NonEmptyMatches() => [new() { WinnerTeam = "teama.png", LoserTeam = "teamb.png", Round = 1, IsBestOfThree = false }];

	private void ReturnsStandings(IReadOnlyList<SwissStanding> standings)
	{
		_standingsCalculator
			.TryCalculateFinalStandings(Arg.Any<IReadOnlyCollection<SimulatorMatchResult>>(), Arg.Any<IReadOnlyDictionary<string, int>>(), out Arg.Any<IReadOnlyList<SwissStanding>>())
			.Returns(x =>
			{
				x[2] = standings;
				return true;
			});
	}

	[Theory]
	[InlineData(Stages.Stage3)]
	[InlineData(Stages.Playoffs)]
	public async Task ApplyAdvancingSeedsAsync_DoesNothing_WhenStageHasNoNextStage(Stages stage)
	{
		// Act
		await _service.ApplyAdvancingSeedsAsync("25", stage, NonEmptyMatches());

		// Assert
		await _seedsTableService.DidNotReceiveWithAnyArgs().GetSeedsInStageAsync(default, default!);
	}

	[Fact]
	public async Task ApplyAdvancingSeedsAsync_DoesNothing_WhenNoCompletedMatches()
	{
		// Act
		await _service.ApplyAdvancingSeedsAsync("25", Stages.Stage1, []);

		// Assert
		await _seedsTableService.DidNotReceiveWithAnyArgs().GetSeedsInStageAsync(default, default!);
	}

	[Fact]
	public async Task ApplyAdvancingSeedsAsync_DoesNothing_WhenCurrentStageSeedingIsIncomplete()
	{
		// Arrange — only 15 of the 16 seeds entered for the current stage.
		string eventId = "25";
		_seedsTableService.GetSeedsInStageAsync(Stages.Stage1, eventId).Returns(AllCurrentSeeds(eventId).Take(15).ToList());

		// Act
		await _service.ApplyAdvancingSeedsAsync(eventId, Stages.Stage1, NonEmptyMatches());

		// Assert
		await _tournamentCachingService.DidNotReceive().GetTournamentTeamsAsync(eventId);
		await _seedsTableService.DidNotReceiveWithAnyArgs().UpsertSeedsAsync(default, default!, default!);
	}

	[Fact]
	public async Task ApplyAdvancingSeedsAsync_DoesNothing_WhenASeededTeamNameDoesNotResolveToATeam()
	{
		// Arrange — one seed's RowKey doesn't match any tournament team.
		string eventId = "25";
		var seeds = AllCurrentSeeds(eventId);
		seeds[0].RowKey = "Some Unknown Team";

		_seedsTableService.GetSeedsInStageAsync(Stages.Stage1, eventId).Returns(seeds);
		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(AllTeams());

		// Act
		await _service.ApplyAdvancingSeedsAsync(eventId, Stages.Stage1, NonEmptyMatches());

		// Assert
		_standingsCalculator.DidNotReceiveWithAnyArgs().TryCalculateFinalStandings(default!, default!, out Arg.Any<IReadOnlyList<SwissStanding>>());
		await _seedsTableService.DidNotReceiveWithAnyArgs().UpsertSeedsAsync(default, default!, default!);
	}

	[Fact]
	public async Task ApplyAdvancingSeedsAsync_DoesNothing_WhenStandingsCalculatorFails()
	{
		// Arrange
		string eventId = "25";
		_seedsTableService.GetSeedsInStageAsync(Stages.Stage1, eventId).Returns(AllCurrentSeeds(eventId));
		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(AllTeams());

		_standingsCalculator
			.TryCalculateFinalStandings(Arg.Any<IReadOnlyCollection<SimulatorMatchResult>>(), Arg.Any<IReadOnlyDictionary<string, int>>(), out Arg.Any<IReadOnlyList<SwissStanding>>())
			.Returns(false);

		// Act
		await _service.ApplyAdvancingSeedsAsync(eventId, Stages.Stage1, NonEmptyMatches());

		// Assert
		await _seedsTableService.DidNotReceiveWithAnyArgs().UpsertSeedsAsync(default, default!, default!);
	}

	[Fact]
	public async Task ApplyAdvancingSeedsAsync_DoesNothing_WhenStandingsDoNotHaveExactlyEightAdvancingTeams()
	{
		// Arrange — only 7 teams reached 3 wins; the underlying data can't be trusted to
		// derive a clean 9-16 split, so this should refuse rather than guess.
		string eventId = "25";
		_seedsTableService.GetSeedsInStageAsync(Stages.Stage1, eventId).Returns(AllCurrentSeeds(eventId));
		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(AllTeams());

		List<SwissStanding> standings =
		[
			new("teama.png", 3, 0, 0, 1), new("teamb.png", 3, 0, 0, 2), new("teamc.png", 3, 1, 0, 3),
			new("teamd.png", 3, 1, 0, 4), new("teame.png", 3, 2, 0, 5), new("teamf.png", 3, 2, 0, 6),
			new("teamg.png", 3, 2, 0, 7), new("teamh.png", 2, 3, 0, 8), new("teami.png", 2, 3, 0, 9),
			new("teamj.png", 1, 3, 0, 10), new("teamk.png", 1, 3, 0, 11), new("teaml.png", 0, 3, 0, 12),
			new("teamm.png", 0, 3, 0, 13), new("teamn.png", 0, 3, 0, 14), new("teamo.png", 0, 3, 0, 15),
			new("teamp.png", 0, 3, 0, 16)
		];
		ReturnsStandings(standings);

		// Act
		await _service.ApplyAdvancingSeedsAsync(eventId, Stages.Stage1, NonEmptyMatches());

		// Assert
		await _seedsTableService.DidNotReceiveWithAnyArgs().UpsertSeedsAsync(default, default!, default!);
	}

	[Fact]
	public async Task ApplyAdvancingSeedsAsync_UpsertsSeeds9Through16_InStandingsOrder_IntoTheNextStage()
	{
		// Arrange
		string eventId = "25";
		_seedsTableService.GetSeedsInStageAsync(Stages.Stage1, eventId).Returns(AllCurrentSeeds(eventId));
		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(AllTeams());

		List<SwissStanding> standings =
		[
			new("teama.png", 3, 0, 10, 1),
			new("teamb.png", 3, 0, 5, 2),
			new("teamc.png", 0, 3, -5, 9),
			new("teamd.png", 3, 1, 3, 3),
			new("teame.png", 3, 1, 2, 4),
			new("teamf.png", 1, 3, -2, 10),
			new("teamg.png", 3, 2, 1, 5),
			new("teamh.png", 3, 2, 0, 6),
			new("teami.png", 3, 2, -1, 7),
			new("teamj.png", 2, 3, -3, 11),
			new("teamk.png", 2, 3, -4, 12),
			new("teaml.png", 0, 3, -6, 13),
			new("teamm.png", 1, 3, -7, 14),
			new("teamn.png", 0, 3, -8, 15),
			new("teamo.png", 0, 3, -9, 16),
			new("teamp.png", 3, 2, -10, 8)
		];
		ReturnsStandings(standings);

		// Act
		await _service.ApplyAdvancingSeedsAsync(eventId, Stages.Stage1, NonEmptyMatches());

		// Assert
		Dictionary<string, int> expected = new()
		{
			["Team A"] = 9,
			["Team B"] = 10,
			["Team D"] = 11,
			["Team E"] = 12,
			["Team G"] = 13,
			["Team H"] = 14,
			["Team I"] = 15,
			["Team P"] = 16
		};

		await _seedsTableService.Received(1).UpsertSeedsAsync(
			Stages.Stage2,
			eventId,
			Arg.Is<IReadOnlyDictionary<string, int>>(d => d.Count == expected.Count && expected.All(kv => d.ContainsKey(kv.Key) && d[kv.Key] == kv.Value)));
	}

	[Fact]
	public async Task ApplyAdvancingSeedsAsync_WritesIntoStage3_WhenGivenStage2()
	{
		// Arrange
		string eventId = "25";
		_seedsTableService.GetSeedsInStageAsync(Stages.Stage2, eventId).Returns(AllCurrentSeeds(eventId));
		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(AllTeams());

		List<SwissStanding> standings =
		[
			new("teama.png", 3, 0, 0, 1), new("teamb.png", 3, 0, 0, 2), new("teamc.png", 3, 1, 0, 3),
			new("teamd.png", 3, 1, 0, 4), new("teame.png", 3, 2, 0, 5), new("teamf.png", 3, 2, 0, 6),
			new("teamg.png", 3, 2, 0, 7), new("teamh.png", 3, 2, 0, 8), new("teami.png", 2, 3, 0, 9),
			new("teamj.png", 1, 3, 0, 10), new("teamk.png", 1, 3, 0, 11), new("teaml.png", 0, 3, 0, 12),
			new("teamm.png", 0, 3, 0, 13), new("teamn.png", 0, 3, 0, 14), new("teamo.png", 0, 3, 0, 15),
			new("teamp.png", 0, 3, 0, 16)
		];
		ReturnsStandings(standings);

		// Act
		await _service.ApplyAdvancingSeedsAsync(eventId, Stages.Stage2, NonEmptyMatches());

		// Assert
		await _seedsTableService.Received(1).UpsertSeedsAsync(Stages.Stage3, eventId, Arg.Any<IReadOnlyDictionary<string, int>>());
	}
}
