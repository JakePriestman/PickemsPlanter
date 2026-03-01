using NSubstitute;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.Simulator;
using PickemsPlanter.Models.Steam;
using PickemsPlanter.Models.StorageAccount;
using Xunit;

namespace PickemsPlanter.Services;

public class SeedsServiceTests
{
	private readonly SeedsService _service;
	private readonly ISeedsTableService _tableService = Substitute.For<ISeedsTableService>();
	private readonly ITournamentCachingService _cachingService = Substitute.For<ITournamentCachingService>();

	public SeedsServiceTests()
	{
		_service = new(_tableService, _cachingService);
	}

	[Fact]
	public async Task GetSeedImagesInStageAsync_ReturnsSeedsWithLogos()
	{
		// Arrange
		string eventId = "25";
		Stages stage = Stages.Stage1;

		List<Team> teams =
		[
			new () { Name = "Existing Team 1", Logo = "existingTeam1", Type = "team" },
			new () { Name = "Existing Team 2", Logo = "existingTeam2", Type = "team" }

		];

		List<Seed> seeds =
		[
			new() { PartitionKey = "25", RowKey = "Existing Team 1" },
			new() { PartitionKey = "25", RowKey = "Existing Team 2" }
		];

		_tableService.GetSeedsInStageAsync(stage, eventId).Returns(seeds);

		_cachingService.GetTournamentTeamsAsync(eventId).Returns(teams);

		// Act
		List<Seeds> result = await _service.GetSeedImagesInStageAsync(stage, eventId);

		// Assert
		Assert.Equal(16, result.Count);
		Assert.Contains(result, s => s.Team == $"{teams[0].Logo}.png");
		Assert.Contains(result, s => s.Team == $"{teams[1].Logo}.png");
		
		for (int i = 0; i < 14; i++)
		{
			Assert.Contains(result, s => s.Team == "unknown.png");
		}
	}
}