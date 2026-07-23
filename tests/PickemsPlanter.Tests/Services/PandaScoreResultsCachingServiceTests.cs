using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using PickemsPlanter.APIs;
using PickemsPlanter.Models.Configurations;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.PandaScore;
using Xunit;

namespace PickemsPlanter.Services;

public class PandaScoreResultsCachingServiceTests
{
	private readonly PandaScoreResultsCachingService _service;
	private readonly IPandaScoreApi _pandaScoreApi = Substitute.For<IPandaScoreApi>();
	private readonly IEventTableService _eventTableService = Substitute.For<IEventTableService>();
	private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
	private readonly IOptionsMonitor<PandaScoreConfig> _config = Substitute.For<IOptionsMonitor<PandaScoreConfig>>();

	public PandaScoreResultsCachingServiceTests()
	{
		_config.CurrentValue.Returns(new PandaScoreConfig { ApiToken = "token", Enabled = true, PollingIntervalMinutes = 10 });

		_service = new(_pandaScoreApi, _eventTableService, _cache, _config);
	}

	private static PandaScoreMatch FinishedMatch(int id, int winnerId) => new()
	{
		Id = id,
		Name = "Round 1: A vs B",
		Status = "finished",
		WinnerId = winnerId,
		Opponents =
		[
			new() { Opponent = new PandaScoreTeam { Id = 1, Name = "A" } },
			new() { Opponent = new PandaScoreTeam { Id = 2, Name = "B" } }
		]
	};

	[Fact]
	public async Task RefreshAllAsync_CachesCompletedMatches_ForMappedStages()
	{
		// Arrange
		Event @event = new()
		{
			Id = "25",
			Name = "Event 1",
			Disabled = false,
			PandaScoreStage1TournamentId = 20708,
			PandaScoreStage2TournamentId = null,
			PandaScoreStage3TournamentId = null
		};

		_eventTableService.GetAllEventsAsync().Returns([@event]);

		List<PandaScoreMatch> matches = [FinishedMatch(1, winnerId: 1)];

		_pandaScoreApi.GetTournamentMatchesAsync(20708).Returns(matches);

		// Act
		await _service.RefreshAllAsync();

		// Assert
		var result = _service.GetCompletedMatches(@event.Id, Stages.Stage1);
		Assert.Single(result);
		await _pandaScoreApi.DidNotReceive().GetTournamentMatchesAsync(Arg.Is<int>(i => i != 20708));
	}

	[Fact]
	public async Task RefreshAllAsync_SkipsStage_WhenNoTournamentIdMapped()
	{
		// Arrange
		Event @event = new() { Id = "25", Name = "Event 1", Disabled = false };

		_eventTableService.GetAllEventsAsync().Returns([@event]);

		// Act
		await _service.RefreshAllAsync();

		// Assert
		await _pandaScoreApi.DidNotReceiveWithAnyArgs().GetTournamentMatchesAsync(default);
		Assert.Empty(_service.GetCompletedMatches(@event.Id, Stages.Stage1));
	}

	[Fact]
	public async Task RefreshAllAsync_SkipsDisabledEvents()
	{
		// Arrange
		Event @event = new() { Id = "25", Name = "Event 1", Disabled = true, PandaScoreStage1TournamentId = 20708 };

		_eventTableService.GetAllEventsAsync().Returns([@event]);

		// Act
		await _service.RefreshAllAsync();

		// Assert
		await _pandaScoreApi.DidNotReceiveWithAnyArgs().GetTournamentMatchesAsync(default);
	}

	[Fact]
	public async Task RefreshAllAsync_DoesNotOverwriteCache_WhenApiCallFails()
	{
		// Arrange
		Event @event = new() { Id = "25", Name = "Event 1", Disabled = false, PandaScoreStage1TournamentId = 20708 };

		_eventTableService.GetAllEventsAsync().Returns([@event]);

		// First poll succeeds and populates the cache...
		_pandaScoreApi.GetTournamentMatchesAsync(20708).Returns([FinishedMatch(1, winnerId: 1)]);
		await _service.RefreshAllAsync();
		Assert.Single(_service.GetCompletedMatches(@event.Id, Stages.Stage1));

		// ...then a later poll fails (null = call failed, not "no matches").
		_pandaScoreApi.GetTournamentMatchesAsync(20708).Returns((IReadOnlyCollection<PandaScoreMatch>?)null);

		// Act
		await _service.RefreshAllAsync();

		// Assert — previously cached value is left in place, not wiped out.
		Assert.Single(_service.GetCompletedMatches(@event.Id, Stages.Stage1));
	}

	[Fact]
	public async Task RefreshAllAsync_OnlyCachesFinishedMatchesWithAWinner()
	{
		// Arrange
		Event @event = new() { Id = "25", Name = "Event 1", Disabled = false, PandaScoreStage1TournamentId = 20708 };

		_eventTableService.GetAllEventsAsync().Returns([@event]);

		List<PandaScoreMatch> matches =
		[
			FinishedMatch(1, winnerId: 1),
			new()
			{
				Id = 2,
				Name = "Round 1: C vs D",
				Status = "not_started",
				WinnerId = null,
				Opponents = []
			}
		];

		_pandaScoreApi.GetTournamentMatchesAsync(20708).Returns(matches);

		// Act
		await _service.RefreshAllAsync();

		// Assert
		var result = _service.GetCompletedMatches(@event.Id, Stages.Stage1);
		var single = Assert.Single(result);
		Assert.Equal(1, single.Id);
	}

	[Fact]
	public void GetCompletedMatches_ReturnsEmpty_WhenNothingCached()
	{
		// Act
		var result = _service.GetCompletedMatches("25", Stages.Stage1);

		// Assert
		Assert.Empty(result);
	}
}
