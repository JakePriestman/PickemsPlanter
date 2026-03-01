using NSubstitute;
using NSubstitute.ReceivedExtensions;
using PickemsPlanter.APIs;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.Steam;
using Xunit;

namespace PickemsPlanter.Services;

public class PickemsServiceTests
{
	private readonly PickemsService _service;
	private readonly IUserPredictionsCachingService _cachingService = Substitute.For<IUserPredictionsCachingService>();
	private readonly ISteamAPI _steamAPI = Substitute.For<ISteamAPI>();
	private readonly ITournamentCachingService _tournamentCachingService = Substitute.For<ITournamentCachingService>();
	private const string BlobContainerUrl = "https://sacs2.blob.core.windows.net/teamimages";

	public PickemsServiceTests()
	{
		_service = new(_cachingService, _steamAPI, _tournamentCachingService);
	}

	[Fact]
	public async Task GetTeamsInStageAsync_ReturnsTeamImages()
	{
		//Arrange
		string eventId = "25";
		Stages stage = Stages.Stage1;

		List<Team> teams =
		[
			new ()
			{
				Logo = "logo.",
				Name = "Team",
				PickId = 1
			}
		];

		Section mockSection = new()
		{
			SectionId = 1,
			Name = "Stage 1",
			Groups =
			[
				new ()
				{
					Name = "Group 1",
					Teams =
					[
						new()
						{
							PickId = 1
						},
						new()
						{
							PickId = 0
						}
					]
				}
			]
		};

		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(teams);
		_tournamentCachingService.GetSectionAsync(eventId, stage).Returns(mockSection);

		//Act
		var result = await _service.GetTeamsInStageAsync(stage, eventId);

		//Assert
		Assert.Equal($"{BlobContainerUrl}/{teams[0].Logo}.png", result[0]);
		Assert.Equal($"{BlobContainerUrl}/unknown.png", result[1]);
	}

	[Fact]
	public async Task GetStageResultsAsync_ReturnsResults()
	{
		//Arrange
		string eventId = "25";
		Stages stage = Stages.Stage1;

		List<Team> teams =
		[
			new ()
			{
				Logo = "logo.",
				Name = "Team",
				PickId = 1
			}
		];

		List<Pick> picks =
		[
			new()
			{
				Index = 1,
				PickIds =
				[
					1
				]
			}
		];

		Section mockSection = new()
		{
			SectionId = 1,
			Name = "Stage 1",
			Groups =
			[
				new ()
				{
					Name = "Group 1",
					Teams =
					[
						new()
						{
							PickId = 1
						},
						new()
						{
							PickId = 0
						}
					],
					Picks =
					[
						new()
						{
							PickIds = [1]
						},
					]
				}
			]
		};

		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(teams);

		_tournamentCachingService.GetSectionAsync(eventId, stage).Returns(mockSection);

		//Act
		var result = await _service.GetStageResultsAsync(stage, eventId);

		//Assert
		Assert.Equal($"{BlobContainerUrl}/{teams[0].Logo}.png", result[0]);
	}

	[Fact]
	public async Task PostStagePickemsAsync_CallsAPIAndRefreshesCache()
	{
		//Arrange
		string eventId = "25";
		string steamId = "1234";
		string mockAuthCode = "MOCK_AUTH_CODE";
		Stages stage = Stages.Stage1;

		string droppedImageData = "[\"logo.png\"]";

		List<string> imageNames = ["logo"];

		List<Team> teams =
		[
			new ()
			{
				Logo = "logo",
				Name = "Team",
				PickId = 1
			}
		];

		Section mockSection = new()
		{
			SectionId = 1,
			Name = "Stage 1",
			Groups =
			[
				new ()
				{
					Name = "Group 1",
					GroupId = 2,
					Teams =
					[
						new()
						{
							PickId = 1
						},
						new()
						{
							PickId = 0
						}
					],
					Picks =
					[
						new()
						{
							PickIds = [1]
						},
					],
					PicksAllowed = true
				}
			]
		};

		_cachingService.CacheUserTeamsAsync(steamId, eventId).Returns(teams);
		_tournamentCachingService.GetSectionAsync(eventId, stage).Returns(mockSection);

		//Act
		await _service.PostStagePickemsAsync(stage, droppedImageData, steamId, eventId, mockAuthCode);

		//Assert
		await _steamAPI.Received(Quantity.Exactly(1)).PostUserPredictionsAsync(Arg.Is<List<string>>(x => x.SequenceEqual(imageNames)), teams, 1, 2, steamId, eventId, mockAuthCode);
		await _cachingService.Received(Quantity.Exactly(1)).RefreshUserPredictionsAsync(steamId, eventId);
	}

	[Fact]
	public async Task GetStagePicksAsync_ReturnsPicksImageNames()
	{
		//Arrange
		string eventId = "25";
		string steamId = "1234";
		Stages stage = Stages.Stage1;

		List<Team> teams =
		[
			new ()
			{
				Logo = "logo",
				Name = "Team",
				PickId = 1
			}
		];

		Section mockSection = new()
		{
			SectionId = 1,
			Name = "Stage 1",
			Groups =
			[
				new ()
				{
					Name = "Group 1",
					GroupId = 1,
					Teams =
					[
						new()
						{
							PickId = 1
						},
						new()
						{
							PickId = 0
						}
					],
					Picks =
					[
						new()
						{
							PickIds = [1]
						},
					],
					PicksAllowed = true
				}
			]
		};

		UserPredictions userPredictions = new UserPredictions()
		{
			Picks =
			[
				new()
				{
					GroupId = 1,
					Pick = 1,
					Index = 0
				}
			]
		};

		_cachingService.CacheUserTeamsAsync(steamId, eventId).Returns(teams);
		_tournamentCachingService.GetSectionAsync(eventId, Stages.Stage1).Returns(mockSection);
		_cachingService.CacheUserPredictionsAsync(steamId, eventId).Returns(userPredictions);

		//Act
		var result = await _service.GetStagePicksAsync(stage, steamId, eventId);

		//Assert
		Assert.Equal($"{BlobContainerUrl}/{teams[0].Logo}.png", result[0]);
	}

	[Fact]
	public async Task GetTeamsInPlayoffsAsync_ReturnsTeamImages()
	{
		//Arrange
		string eventId = "25";

		List<Team> teams =
		[
			new ()
			{
				Logo = "logo.",
				Name = "Team",
				PickId = 1
			}
		];

		List<Section> sections =
		[
			new()
			{
				SectionId = 1,
				Name = "Playoffs",
				Groups =
				[
					new ()
					{
						Name = "Group 1",
						Teams =
						[
							new()
							{
								PickId = 1
							},
							new()
							{
								PickId = 0
							}
						]
					}
				]
			}
		];

		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(teams);
		_tournamentCachingService.GetPlayoffsAsync(eventId).Returns(sections);

		//Act
		var result = await _service.GetTeamsInPlayoffsAsync(eventId);

		//Assert
		Assert.Equal($"{BlobContainerUrl}/{teams[0].Logo}.png", result[0]);
		Assert.Equal($"{BlobContainerUrl}/unknown.png", result[1]);
	}

	[Fact]
	public async Task GetPlayoffPicksAsync_ReturnsPicksImageNames()
	{
		//Arrange
		string eventId = "25"; 
		string steamId = "1234";

		List<Team> teams =
		[
			new ()
			{
				Logo = "logo.",
				Name = "Team",
				PickId = 1
			}
		];

		List<Section> sections =
		[
			new()
			{
				SectionId = 1,
				Name = "Playoffs",
				Groups =
				[
					new ()
					{
						Name = "Group 1",
						GroupId = 1,
						Teams =
						[
							new()
							{
								PickId = 1
							},
							new()
							{
								PickId = 0
							}
						]
					}
				]
			}
		];

		UserPredictions userPredictions = new UserPredictions()
		{
			Picks =
			[
				new()
				{
					GroupId = 1,
					Pick = 1,
					Index = 0
				}
			]
		};

		_cachingService.CacheUserTeamsAsync(steamId, eventId).Returns(teams);
		_tournamentCachingService.GetPlayoffsAsync(eventId).Returns(sections);
		_cachingService.CacheUserPredictionsAsync(steamId, eventId).Returns(userPredictions);

		//Act
		var result = await _service.GetPlayoffPicksAsync(steamId, eventId);

		//Assert
		Assert.Equal($"{BlobContainerUrl}/{teams[0].Logo}.png", result[0]);
	}

	[Fact]
	public async Task GetPlayoffResultsAsync_ReturnsResults()
	{
		//Arrange
		string eventId = "25";

		List<Team> teams =
		[
			new ()
			{
				Logo = "logo.",
				Name = "Team",
				PickId = 1
			}
		];

		List<Pick> picks =
		[
			new()
			{
				Index = 1,
				PickIds =
				[
					1
				]
			}
		];

		List<Section> sections =
		[
			new()
			{
				SectionId = 1,
				Name = "Playoffs",
				Groups =
				[
					new ()
					{
						Name = "Group 1",
						GroupId = 1,
						Teams =
						[
							new()
							{
								PickId = 1
							},
							new()
							{
								PickId = 0
							}
						],
						Picks = 
						[
							new()
							{
								PickIds = [1]
							}
						]
					}
				]
			}
		];

		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(teams);

		_tournamentCachingService.GetPlayoffsAsync(eventId).Returns(sections);

		//Act
		var result = await _service.GetPlayoffResultsAsync(eventId);

		//Assert
		Assert.Equal($"{BlobContainerUrl}/{teams[0].Logo}.png", result[0]);
	}

	[Fact]
	public async Task PostPlayoffPickemsAsync_CallsAPIAndRefreshesCache()
	{
		//Arrange
		string eventId = "25";
		string steamId = "1234";
		string mockAuthCode = "MOCK_AUTH_CODE";

		string droppedImageData = "[\"logo.png\"]";

		List<string> imageNames = ["logo"];

		List<Team> teams =
		[
			new ()
			{
				Logo = "logo",
				Name = "Team",
				PickId = 1
			}
		];

		List<Section> sections =
		[
			new()
			{
				SectionId = 1,
				Name = "Playoffs",
				Groups =
				[
					new ()
					{
						Name = "Group 1",
						GroupId = 1,
						Teams =
						[
							new()
							{
								PickId = 1
							},
							new()
							{
								PickId = 0
							}
						],
						Picks =
						[
							new()
							{
								PickIds = [1]
							}
						],
						PicksAllowed = true
					}
				]
			}
		];

		_cachingService.CacheUserTeamsAsync(steamId, eventId).Returns(teams);
		_tournamentCachingService.GetPlayoffsAsync(eventId).Returns(sections);

		//Act
		await _service.PostPlayoffPickemsAsync(droppedImageData, steamId, eventId, mockAuthCode);

		//Assert
		await _steamAPI.Received(Quantity.Exactly(1)).PostPlayoffPredictionsAsync(Arg.Is<List<string>>(x => x.SequenceEqual(imageNames)), teams, sections, steamId, eventId, mockAuthCode);
		await _cachingService.Received(Quantity.Exactly(1)).RefreshUserPredictionsAsync(steamId, eventId);
	}
}
