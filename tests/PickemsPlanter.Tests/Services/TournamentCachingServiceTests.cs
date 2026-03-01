using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using PickemsPlanter.APIs;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.Steam;
using Xunit;

namespace PickemsPlanter.Services;


public class TournamentCachingServiceTests
{
	private readonly TournamentCachingService _service;
	private readonly ISteamAPI _steamAPI = Substitute.For<ISteamAPI>();
	private readonly IMemoryCache _cache = Substitute.For<IMemoryCache>();

	public TournamentCachingServiceTests()
	{
		_service = new(_steamAPI, _cache);
	}

	[Fact]
	public async Task GetSectionAsync_ReturnsSectionFromCache_WhenExists()
	{
		//Arrange
		string eventId = "25";
		Stages stage = Stages.Stage1;
		string key = $"TOURNAMENT_{eventId}_{stage}";

		Section mockSection = new ()
		{
			SectionId = 1,
			Name = "Stage 1",
			Groups = []
		};

		_cache.TryGetValue(key, out Section? section).Returns(x =>
		{
			x[1] = mockSection;
			return true;
		});

		//Act
		var result = await _service.GetSectionAsync(eventId, stage);

		//Assert
		Assert.Equal(mockSection.Name, result.Name);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(key, out Section? _);
		await _steamAPI.DidNotReceive().GetTournamentLayoutAsync(eventId);
	}

	[Fact]
	public async Task GetSectionAsync_CallsAPI_WhenNotExistsInCache()
	{
		//Arrange
		string eventId = "25";
		Stages stage = Stages.Stage1;
		string key = $"TOURNAMENT_{eventId}_{stage}";

		Section mockSection = new()
		{
			SectionId = 1,
			Name = "Stage 1",
			Groups = []
		};

		TournamentLayout layout = new TournamentLayout()
		{
			Name = "Tounrment 2",
			Teams = [],
			Sections =
			[
				mockSection
			]
		};

		_cache.TryGetValue(key, out Section? section).Returns(x =>
		{
			x[1] = null;
			return false;
		});

		_steamAPI.GetTournamentLayoutAsync(eventId).Returns(new GetResult<TournamentLayout>()
		{
			Result = layout,
		});

		//Act
		var result = await _service.GetSectionAsync(eventId, stage);

		//Assert
		Assert.Equal(mockSection.Name, result.Name);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(key, out Section? _);
		await _steamAPI.Received(Quantity.Exactly(1)).GetTournamentLayoutAsync(eventId);
		_cache.Received(Quantity.Exactly(1)).Set(key, mockSection, TimeSpan.FromMinutes(10));
	}

	[Fact]
	public async Task GetTournamentTeamsAsync_ReturnsSectionFromCache_WhenExists()
	{
		//Arrange
		string eventId = "25";
		string key = $"TOURNAMENT_{eventId}_TEAMS";

		List<Team> teams =
		[
			new Team
			{
				Name = "Team A",
				Logo = "logo_a.png",
				Type = "Team",
			},
			new Team
			{
				Name = "Team B",
				Logo = "logo_b.png",
				Type = "Team",
			}
		];

		_cache.TryGetValue(key, out IReadOnlyCollection<Team>? section).Returns(x =>
		{
			x[1] = teams;
			return true;
		});

		//Act
		var result = await _service.GetTournamentTeamsAsync(eventId);

		//Assert
		Assert.Equal(teams.Count, result.Count);
		Assert.Equal(teams[0].Name, result.ToList()[0].Name);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(key, out IReadOnlyCollection<Team>? _);
		await _steamAPI.DidNotReceive().GetTournamentLayoutAsync(eventId);
	}

	[Fact]
	public async Task GetTournamentTeamsAsync_CallsAPI_WhenNotExistsInCache()
	{
		//Arrange
		string eventId = "25";
		string key = $"TOURNAMENT_{eventId}_TEAMS";

		List<Team> teams =
		[
			new Team
			{
				Name = "Team A",
				Logo = "logo_a.png",
				Type = "Team",
			},
			new Team
			{
				Name = "Team B",
				Logo = "logo_b.png",
				Type = "Team",
			}
		];

		TournamentLayout layout = new TournamentLayout()
		{
			Name = "Tounrment 2",
			Teams = teams,
			Sections = []
		};

		_cache.TryGetValue(key, out IReadOnlyCollection<Team>? section).Returns(x =>
		{
			x[1] = null;
			return false;
		});

		_steamAPI.GetTournamentLayoutAsync(eventId).Returns(new GetResult<TournamentLayout>()
		{
			Result = layout,
		});

		//Act
		var result = await _service.GetTournamentTeamsAsync(eventId);

		//Assert
		Assert.Equal(teams.Count, result.Count);
		Assert.Equal(teams[0].Name, result.ToList()[0].Name);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(key, out IReadOnlyCollection<Team>? _);
		await _steamAPI.Received(Quantity.Exactly(1)).GetTournamentLayoutAsync(eventId);
		_cache.Received(Quantity.Exactly(1)).Set(key, teams);
	}

	[Fact]
	public async Task GetPlayoffsAsync_ReturnsSectionFromCache_WhenExists()
	{
		//Arrange
		string eventId = "25";
		string key = $"TOURNAMENT_{eventId}_{Stages.Playoffs}";

		List<Section> mockPlayoffs =
		[
			new()
			{
				SectionId = 1,
				Name = "Quarter Finals",
				Groups = []
			},
			new()
			{
				SectionId = 2,
				Name = "Semi Finals",
				Groups = []
			},
		];

		_cache.TryGetValue(key, out IReadOnlyCollection<Section>? playoffs).Returns(x =>
		{
			x[1] = mockPlayoffs;
			return true;
		});

		//Act
		var result = await _service.GetPlayoffsAsync(eventId);

		//Assert
		Assert.Equal(mockPlayoffs.Count, result.Count);
		Assert.Equal(mockPlayoffs[0].Name, result.ToList()[0].Name);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(key, out Section? _);
		await _steamAPI.DidNotReceive().GetTournamentLayoutAsync(eventId);
	}

	[Fact]
	public async Task GetPlayoffsAsync_CallsAPI_WhenNotExistsInCache()
	{
		//Arrange
		string eventId = "25";
		string key = $"TOURNAMENT_{eventId}_{Stages.Playoffs}";

		List<Section> mockPlayoffs =
		[
			new()
			{
				SectionId = 1,
				Name = "Stage 1",
				Groups = []
			},
			new()
			{
				SectionId = 2,
				Name = "Stage 2",
				Groups = []
			},
			new()
			{
				SectionId = 3,
				Name = "Stage 3",
				Groups = []
			},
			new()
			{
				SectionId = 4,
				Name = "Quarter Finals",
				Groups = []
			},
			new()
			{
				SectionId = 5,
				Name = "Semi Finals",
				Groups = []
			},
		];

		TournamentLayout layout = new TournamentLayout()
		{
			Name = "Tounrment 2",
			Teams = [],
			Sections = mockPlayoffs
		};

		_cache.TryGetValue(key, out IReadOnlyCollection<Section>? playoffs).Returns(x =>
		{
			x[1] = null;
			return false;
		});

		_steamAPI.GetTournamentLayoutAsync(eventId).Returns(new GetResult<TournamentLayout>()
		{
			Result = layout,
		});

		//Act
		var result = await _service.GetPlayoffsAsync(eventId);

		//Assert
		Assert.Equal((mockPlayoffs.Count)-3, result.Count);
		Assert.Equal(mockPlayoffs[3].Name, result.ToList()[0].Name);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(key, out IReadOnlyCollection<Section>? _);
		await _steamAPI.Received(Quantity.Exactly(1)).GetTournamentLayoutAsync(eventId);
		_cache.Received(Quantity.Exactly(1)).Set(key, playoffs, TimeSpan.FromMinutes(10));
	}

	[Fact]
	public async Task GetFirstActiveStageOrDefaultAsync_ReturnsFirstActiveStage_WhenStageIsActive()
	{
		//Arrange
		string eventId = "25";
		string stage1Key = $"TOURNAMENT_{eventId}_{Stages.Stage1}";
		string stage2Key = $"TOURNAMENT_{eventId}_{Stages.Stage2}";
		List<Section> sections =
		[
			new()
			{
				SectionId = 1,
				Name = "Stage 1",
				Groups =  [
					new Group
					{
						GroupId = 1,
						Name = "Group 1",
						PicksAllowed = false,
					},
					new Group
					{
						GroupId = 2,
						Name = "Group 2",
						PicksAllowed = false,
					}
				]
			},
			new()
			{
				SectionId = 2,
				Name = "Stage 2",
				Groups = [
					new Group
					{
						GroupId = 1,
						Name = "Group 1",
						PicksAllowed = false,
					},
					new Group
					{
						GroupId = 2,
						Name = "Group 2",
						PicksAllowed = true,
					}
				]
			}
		];

		_cache.TryGetValue(stage1Key, out Section? _).Returns(x =>
		{
			x[1] = sections[0];
			return true;
		});

		_cache.TryGetValue(stage2Key, out Section? _).Returns(x =>
		{
			x[1] = sections[1];
			return true;
		});

		//Act
		var result = await _service.GetFirstActiveStageOrDefaultAsync(eventId);

		//Assert
		Assert.Equal(Stages.Stage2, result);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(stage1Key, out Section? _);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(stage2Key, out Section? _);
		_cache.DidNotReceive().TryGetValue($"TOURNAMENT_{eventId}_{Stages.Playoffs}", out IReadOnlyCollection<Section>? _);
	}

	[Fact]
	public async Task GetFirstActiveStageOrDefaultAsync_ReturnsDefaultStage_WhenNoStageIsActive()
	{
		//Arrange
		string eventId = "25";
		string stage1Key = $"TOURNAMENT_{eventId}_{Stages.Stage1}";
		string stage2Key = $"TOURNAMENT_{eventId}_{Stages.Stage2}";
		string stage3Key = $"TOURNAMENT_{eventId}_{Stages.Stage3}";
		string playoffsKey = $"TOURNAMENT_{eventId}_{Stages.Playoffs}";
		List<Section> sections =
		[
			new()
			{
				SectionId = 1,
				Name = "Stage 1",
				Groups =  [
					new Group
					{
						GroupId = 1,
						Name = "Group 1",
						PicksAllowed = false,
					},
					new Group
					{
						GroupId = 2,
						Name = "Group 2",
						PicksAllowed = false,
					}
				]
			},
			new()
			{
				SectionId = 2,
				Name = "Stage 2",
				Groups = [
					new Group
					{
						GroupId = 1,
						Name = "Group 1",
						PicksAllowed = false,
					},
					new Group
					{
						GroupId = 2,
						Name = "Group 2",
						PicksAllowed = false,
					}
				]
			},
			new()
			{
				SectionId = 3,
				Name = "Stage 3",
				Groups = [
					new Group
					{
						GroupId = 1,
						Name = "Group 1",
						PicksAllowed = false,
					},
					new Group
					{
						GroupId = 2,
						Name = "Group 2",
						PicksAllowed = false,
					}
				]
			},
			new()
			{
				SectionId = 4,
				Name = "Playoffs",
				Groups = [
					new Group
					{
						GroupId = 1,
						Name = "Group 1",
						PicksAllowed = false,
					},
					new Group
					{
						GroupId = 2,
						Name = "Group 2",
						PicksAllowed = false,
					}
				]
			}
		];

		_cache.TryGetValue(stage1Key, out Section? _).Returns(x =>
		{
			x[1] = sections[0];
			return true;
		});

		_cache.TryGetValue(stage2Key, out Section? _).Returns(x =>
		{
			x[1] = sections[1];
			return true;
		});

		_cache.TryGetValue(stage3Key, out Section? _).Returns(x =>
		{
			x[1] = sections[2];
			return true;
		});

		_cache.TryGetValue(playoffsKey, out IReadOnlyCollection<Section>? _).Returns(x =>
		{
			x[1] = sections;
			return true;
		});

		//Act
		var result = await _service.GetFirstActiveStageOrDefaultAsync(eventId);

		//Assert
		Assert.Equal(Stages.Stage1, result);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(stage1Key, out Section? _);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(stage2Key, out Section? _);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(stage3Key, out Section? _);
		_cache.Received(Quantity.Exactly(1)).TryGetValue($"TOURNAMENT_{eventId}_{Stages.Playoffs}", out IReadOnlyCollection<Section>? _);
	}
}
