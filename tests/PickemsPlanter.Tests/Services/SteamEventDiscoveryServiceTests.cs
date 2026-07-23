using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PickemsPlanter.APIs;
using PickemsPlanter.Models.Configurations;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.PandaScore;
using PickemsPlanter.Models.Steam;
using Xunit;

namespace PickemsPlanter.Services;

public class SteamEventDiscoveryServiceTests
{
	private readonly SteamEventDiscoveryService _service;
	private readonly ISteamAPI _steamAPI = Substitute.For<ISteamAPI>();
	private readonly IEventTableService _eventTableService = Substitute.For<IEventTableService>();
	private readonly IPandaScoreApi _pandaScoreApi = Substitute.For<IPandaScoreApi>();
	private readonly IOptionsMonitor<EventDiscoveryConfig> _config = Substitute.For<IOptionsMonitor<EventDiscoveryConfig>>();

	public SteamEventDiscoveryServiceTests()
	{
		_config.CurrentValue.Returns(new EventDiscoveryConfig { Enabled = true, PollingIntervalMinutes = 60, LookaheadCount = 5 });

		_service = new(_steamAPI, _eventTableService, _pandaScoreApi, _config);
	}

	private static GetResult<TournamentLayout> Layout(string name) => new()
	{
		Result = new TournamentLayout
		{
			Event = 0,
			Name = name,
			Sections = [],
			Teams = []
		}
	};

	[Fact]
	public async Task RunDiscoveryAsync_DisablesEvent_WhenLayoutNoLongerFound()
	{
		// Arrange
		Event @event = new() { Id = "20", Name = "Old Major", Disabled = false };

		_eventTableService.GetAllEventsAsync().Returns([@event]);
		// Covers both the retire-check on "20" and the discovery-phase lookahead probes (21-25).
		_steamAPI.GetTournamentLayoutAsync(Arg.Any<string>()).ThrowsAsync<KeyNotFoundException>();

		// Act
		await _service.RunDiscoveryAsync();

		// Assert
		await _eventTableService.Received(1).UpsertEventAsync("20", "Old Major", true);
	}

	[Fact]
	public async Task RunDiscoveryAsync_DoesNotDisableEvent_WhenLayoutStillFound()
	{
		// Arrange
		Event @event = new() { Id = "26", Name = "IEM Cologne 2026 CS2 Major Championship", Disabled = false };

		_eventTableService.GetAllEventsAsync().Returns([@event]);
		_steamAPI.GetTournamentLayoutAsync("26").Returns(Layout("IEM Cologne 2026 CS2 Major Championship"));
		_steamAPI.GetTournamentLayoutAsync(Arg.Is<string>(s => s != "26")).ThrowsAsync<KeyNotFoundException>();

		// Act
		await _service.RunDiscoveryAsync();

		// Assert
		await _eventTableService.DidNotReceive().UpsertEventAsync("26", Arg.Any<string>(), true);
	}

	[Fact]
	public async Task RunDiscoveryAsync_DiscoversNewEvent_WhenNextIdResolves()
	{
		// Arrange
		Event @event = new() { Id = "26", Name = "IEM Cologne 2026 CS2 Major Championship", Disabled = false };

		_eventTableService.GetAllEventsAsync().Returns([@event]);
		_steamAPI.GetTournamentLayoutAsync("26").Returns(Layout("IEM Cologne 2026 CS2 Major Championship"));
		_steamAPI.GetTournamentLayoutAsync("27").Returns(Layout("New Major 2027"));
		_steamAPI.GetTournamentLayoutAsync(Arg.Is<string>(s => s != "26" && s != "27")).ThrowsAsync<KeyNotFoundException>();

		// Act
		await _service.RunDiscoveryAsync();

		// Assert
		await _eventTableService.Received(1).UpsertEventAsync("27", "New Major 2027", false);
	}

	[Fact]
	public async Task RunDiscoveryAsync_DoesNotProbeBeyondLookaheadWindow()
	{
		// Arrange
		Event @event = new() { Id = "26", Name = "IEM Cologne 2026 CS2 Major Championship", Disabled = false };

		_eventTableService.GetAllEventsAsync().Returns([@event]);
		_steamAPI.GetTournamentLayoutAsync(Arg.Any<string>()).ThrowsAsync<KeyNotFoundException>();

		// Act
		await _service.RunDiscoveryAsync();

		// Assert — lookahead is 5, so only 27..31 should ever be probed, never 32.
		await _steamAPI.DidNotReceive().GetTournamentLayoutAsync("32");
		await _steamAPI.Received(1).GetTournamentLayoutAsync("31");
	}

	[Fact]
	public async Task RunDiscoveryAsync_ContinuesPastA404WithinLookaheadWindow()
	{
		// Arrange
		Event @event = new() { Id = "26", Name = "IEM Cologne 2026 CS2 Major Championship", Disabled = false };

		_eventTableService.GetAllEventsAsync().Returns([@event]);
		_steamAPI.GetTournamentLayoutAsync("26").Returns(Layout("IEM Cologne 2026 CS2 Major Championship"));
		_steamAPI.GetTournamentLayoutAsync("27").ThrowsAsync<KeyNotFoundException>();
		_steamAPI.GetTournamentLayoutAsync("28").Returns(Layout("Gap Major 2028"));
		_steamAPI.GetTournamentLayoutAsync(Arg.Is<string>(s => s != "26" && s != "27" && s != "28")).ThrowsAsync<KeyNotFoundException>();

		// Act
		await _service.RunDiscoveryAsync();

		// Assert
		await _eventTableService.Received(1).UpsertEventAsync("28", "Gap Major 2028", false);
	}

	[Fact]
	public async Task RunDiscoveryAsync_AutoResolvesPandaScoreMapping_WhenExactlyOneYearMatch()
	{
		// Arrange
		Event @event = new() { Id = "26", Name = "Existing", Disabled = false };

		_eventTableService.GetAllEventsAsync().Returns([@event]);
		_steamAPI.GetTournamentLayoutAsync("26").Returns(Layout("Existing"));
		_steamAPI.GetTournamentLayoutAsync("27").Returns(Layout("IEM Cologne 2026 CS2 Major Championship"));
		_steamAPI.GetTournamentLayoutAsync(Arg.Is<string>(s => s != "26" && s != "27")).ThrowsAsync<KeyNotFoundException>();

		PandaScoreSeries series = new()
		{
			Id = 10488,
			Name = "Cologne Major",
			Year = 2026,
			Tournaments =
			[
				new() { Id = 20708, Name = "Stage 1" },
				new() { Id = 20709, Name = "Stage 2" },
				new() { Id = 21115, Name = "Stage 3" },
				new() { Id = 20710, Name = "Playoffs" }
			]
		};

		PandaScoreSeries otherYear = new() { Id = 9495, Name = "Cologne", Year = 2025, Tournaments = [] };

		_pandaScoreApi.SearchSeriesAsync("Cologne").Returns([series, otherYear]);
		_pandaScoreApi.SearchSeriesAsync(Arg.Is<string>(s => s != "Cologne")).Returns([]);

		// Act
		await _service.RunDiscoveryAsync();

		// Assert
		await _eventTableService.Received(1).SetPandaScoreTournamentIdsAsync("27", 20708, 20709, 21115);
	}

	[Fact]
	public async Task RunDiscoveryAsync_DoesNotResolveMapping_WhenAmbiguousYearMatches()
	{
		// Arrange
		Event @event = new() { Id = "26", Name = "Existing", Disabled = false };

		_eventTableService.GetAllEventsAsync().Returns([@event]);
		_steamAPI.GetTournamentLayoutAsync("26").Returns(Layout("Existing"));
		_steamAPI.GetTournamentLayoutAsync("27").Returns(Layout("IEM Cologne 2026 CS2 Major Championship"));
		_steamAPI.GetTournamentLayoutAsync(Arg.Is<string>(s => s != "26" && s != "27")).ThrowsAsync<KeyNotFoundException>();

		PandaScoreSeries seriesA = new() { Id = 1, Name = "Cologne Major", Year = 2026, Tournaments = [] };
		PandaScoreSeries seriesB = new() { Id = 2, Name = "IEM Cologne", Year = 2026, Tournaments = [] };

		_pandaScoreApi.SearchSeriesAsync(Arg.Any<string>()).Returns([seriesA, seriesB]);

		// Act
		await _service.RunDiscoveryAsync();

		// Assert
		await _eventTableService.DidNotReceiveWithAnyArgs().SetPandaScoreTournamentIdsAsync(default!, default, default, default);
	}

	[Fact]
	public async Task RunDiscoveryAsync_ResolvesMapping_ForAlreadyKnownEventMissingIt()
	{
		// Arrange — event "26" already exists in the table (e.g. it predates this feature),
		// so it's never a "candidate" in the discovery loop, but it's still missing its mapping.
		Event @event = new() { Id = "26", Name = "IEM Cologne 2026 CS2 Major Championship", Disabled = false };

		_eventTableService.GetAllEventsAsync().Returns([@event]);
		_steamAPI.GetTournamentLayoutAsync(Arg.Any<string>()).ThrowsAsync<KeyNotFoundException>();
		_steamAPI.GetTournamentLayoutAsync("26").Returns(Layout("IEM Cologne 2026 CS2 Major Championship"));

		PandaScoreSeries series = new()
		{
			Id = 10488,
			Name = "Cologne Major",
			Year = 2026,
			Tournaments =
			[
				new() { Id = 20708, Name = "Stage 1" },
				new() { Id = 20709, Name = "Stage 2" },
				new() { Id = 21115, Name = "Stage 3" }
			]
		};

		_pandaScoreApi.SearchSeriesAsync(Arg.Any<string>()).Returns([series]);

		// Act
		await _service.RunDiscoveryAsync();

		// Assert
		await _eventTableService.Received(1).SetPandaScoreTournamentIdsAsync("26", 20708, 20709, 21115);
	}

	[Fact]
	public async Task RunDiscoveryAsync_DoesNotResolveMapping_ForAlreadyKnownEventWithFullMapping()
	{
		// Arrange
		Event @event = new()
		{
			Id = "26",
			Name = "IEM Cologne 2026 CS2 Major Championship",
			Disabled = false,
			PandaScoreStage1TournamentId = 20708,
			PandaScoreStage2TournamentId = 20709,
			PandaScoreStage3TournamentId = 21115
		};

		_eventTableService.GetAllEventsAsync().Returns([@event]);
		_steamAPI.GetTournamentLayoutAsync(Arg.Any<string>()).ThrowsAsync<KeyNotFoundException>();
		_steamAPI.GetTournamentLayoutAsync("26").Returns(Layout("IEM Cologne 2026 CS2 Major Championship"));

		// Act
		await _service.RunDiscoveryAsync();

		// Assert
		await _pandaScoreApi.DidNotReceiveWithAnyArgs().SearchSeriesAsync(default!);
	}

	[Fact]
	public async Task RunDiscoveryAsync_DoesNotResolveMapping_WhenNoYearInName()
	{
		// Arrange
		Event @event = new() { Id = "26", Name = "Existing", Disabled = false };

		_eventTableService.GetAllEventsAsync().Returns([@event]);
		_steamAPI.GetTournamentLayoutAsync("26").Returns(Layout("Existing"));
		_steamAPI.GetTournamentLayoutAsync("27").Returns(Layout("A Major Without A Year"));
		_steamAPI.GetTournamentLayoutAsync(Arg.Is<string>(s => s != "26" && s != "27")).ThrowsAsync<KeyNotFoundException>();

		// Act
		await _service.RunDiscoveryAsync();

		// Assert
		await _eventTableService.Received(1).UpsertEventAsync("27", "A Major Without A Year", false);
		await _pandaScoreApi.DidNotReceiveWithAnyArgs().SearchSeriesAsync(default!);
	}
}
