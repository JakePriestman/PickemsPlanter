using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using PickemsPlanter.APIs;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.Steam;
using System.Text.Json;
using Xunit;

namespace PickemsPlanter.Services;

public class StartupCachingServiceTests
{
	private readonly StartupCachingService _service;
	private readonly IMemoryCache _cache = Substitute.For<IMemoryCache>();
	private readonly IEventTableService _eventTableService = Substitute.For<IEventTableService>();
	private readonly ISteamAPI _steamAPI = Substitute.For<ISteamAPI>();

	public StartupCachingServiceTests()
	{
		_service = new StartupCachingService(_cache, _steamAPI, _eventTableService);
	}
	
	[Fact]
	public async Task StartAsync_CachesEvents()
	{
		//Arrange
		List<Event> events =
		[
			new () { Id = "25", Name = "Event 1" },
		];

		_eventTableService.GetAllEventsAsync().Returns(events);

		var json = File.ReadAllText("../../../Services/tournamentLayout.json");

		JsonSerializerOptions options = new ()
		{
			PropertyNameCaseInsensitive = true
		};

		GetResult<TournamentLayout> layout = JsonSerializer.Deserialize<GetResult<TournamentLayout>>(json, options)!;

		_steamAPI.GetTournamentLayoutAsync(events[0].Id).Returns(layout);

		//Act
		await _service.StartAsync(CancellationToken.None);

		//Assert
		_cache.Received(Quantity.Exactly(1)).Set($"TOURNAMENT_{events[0].Id}_{Stages.Stage1}", layout.Result.Sections[(int)Stages.Stage1]);
		_cache.Received(Quantity.Exactly(1)).Set($"TOURNAMENT_{events[0].Id}_{Stages.Stage2}", layout.Result.Sections[(int)Stages.Stage2]);
		_cache.Received(Quantity.Exactly(1)).Set($"TOURNAMENT_{events[0].Id}_{Stages.Stage3}", layout.Result.Sections[(int)Stages.Stage3]);

		layout.Result.Sections.Remove(layout.Result.Sections[0]);
		layout.Result.Sections.Remove(layout.Result.Sections[0]);
		layout.Result.Sections.Remove(layout.Result.Sections[0]);

		_cache.Received(Quantity.Exactly(1)).Set($"TOURNAMENT_{events[0].Id}_{Stages.Playoffs}", layout.Result.Sections);
	}
}
