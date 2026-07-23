using NSubstitute;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.PandaScore;
using PickemsPlanter.Models.Steam;
using Xunit;

namespace PickemsPlanter.Services;

public class PandaScoreResultsServiceTests
{
	private readonly PandaScoreResultsService _service;
	private readonly IPandaScoreResultsCachingService _cachingService = Substitute.For<IPandaScoreResultsCachingService>();
	private readonly ITournamentCachingService _tournamentCachingService = Substitute.For<ITournamentCachingService>();

	public PandaScoreResultsServiceTests()
	{
		_service = new(_cachingService, _tournamentCachingService);
	}

	[Fact]
	public async Task GetCompletedMatchesAsync_ReturnsEmpty_WhenNoCachedMatches()
	{
		// Arrange
		string eventId = "25";
		Stages stage = Stages.Stage1;

		_cachingService.GetCompletedMatches(eventId, stage).Returns([]);

		// Act
		var result = await _service.GetCompletedMatchesAsync(eventId, stage);

		// Assert
		Assert.Empty(result);
		await _tournamentCachingService.DidNotReceive().GetTournamentTeamsAsync(eventId);
	}

	[Fact]
	public async Task GetCompletedMatchesAsync_ResolvesWinnerAndLoserToLogoFileNames()
	{
		// Arrange
		string eventId = "25";
		Stages stage = Stages.Stage1;

		List<Team> teams =
		[
			new() { Name = "BIG", Logo = "big" },
			new() { Name = "NRG", Logo = "nrg" }
		];

		PandaScoreMatch match = new()
		{
			Id = 1,
			Name = "Round 5: NRG vs BIG",
			Status = "finished",
			WinnerId = 3249,
			Opponents =
			[
				new() { Opponent = new PandaScoreTeam { Id = 3256, Name = "NRG" } },
				new() { Opponent = new PandaScoreTeam { Id = 3249, Name = "BIG" } }
			]
		};

		_cachingService.GetCompletedMatches(eventId, stage).Returns([match]);
		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(teams);

		// Act
		var result = await _service.GetCompletedMatchesAsync(eventId, stage);

		// Assert
		var single = Assert.Single(result);
		Assert.Equal("big.png", single.WinnerTeam);
		Assert.Equal("nrg.png", single.LoserTeam);
		Assert.Equal(5, single.Round);
	}

	[Fact]
	public async Task GetCompletedMatchesAsync_SkipsMatch_WhenTeamNameUnresolved()
	{
		// Arrange
		string eventId = "25";
		Stages stage = Stages.Stage1;

		List<Team> teams =
		[
			new() { Name = "BIG", Logo = "big" }
		];

		PandaScoreMatch match = new()
		{
			Id = 1,
			Name = "Round 1: BIG vs Unknown Team",
			Status = "finished",
			WinnerId = 3249,
			Opponents =
			[
				new() { Opponent = new PandaScoreTeam { Id = 3249, Name = "BIG" } },
				new() { Opponent = new PandaScoreTeam { Id = 9999, Name = "Unknown Team" } }
			]
		};

		_cachingService.GetCompletedMatches(eventId, stage).Returns([match]);
		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(teams);

		// Act
		var result = await _service.GetCompletedMatchesAsync(eventId, stage);

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public async Task GetCompletedMatchesAsync_SkipsMatch_WhenNameHasNoRoundPrefix()
	{
		// Arrange
		string eventId = "25";
		Stages stage = Stages.Stage1;

		List<Team> teams =
		[
			new() { Name = "BIG", Logo = "big" },
			new() { Name = "NRG", Logo = "nrg" }
		];

		PandaScoreMatch match = new()
		{
			Id = 1,
			Name = "Grand final: BIG vs NRG",
			Status = "finished",
			WinnerId = 3249,
			Opponents =
			[
				new() { Opponent = new PandaScoreTeam { Id = 3249, Name = "BIG" } },
				new() { Opponent = new PandaScoreTeam { Id = 3256, Name = "NRG" } }
			]
		};

		_cachingService.GetCompletedMatches(eventId, stage).Returns([match]);
		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(teams);

		// Act
		var result = await _service.GetCompletedMatchesAsync(eventId, stage);

		// Assert
		Assert.Empty(result);
	}
}
