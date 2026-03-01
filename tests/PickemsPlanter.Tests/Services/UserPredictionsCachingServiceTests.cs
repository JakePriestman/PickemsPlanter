using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using PickemsPlanter.APIs;
using PickemsPlanter.Models.Steam;
using PickemsPlanter.Models.TableStorage;
using Xunit;

namespace PickemsPlanter.Services;

public class UserPredictionsCachingServiceTests
{
	private readonly UserPredictionsCachingService _service;
	private readonly IMemoryCache _cache = Substitute.For<IMemoryCache>();
	private readonly ISteamAPI _steamAPI = Substitute.For<ISteamAPI>();
	private readonly ITournamentCachingService _tournamentCachingService = Substitute.For<ITournamentCachingService>();
	private readonly IUserEventsTableService _tableService = Substitute.For<IUserEventsTableService>();
	public UserPredictionsCachingServiceTests()
	{
		_service = new(_cache, _steamAPI, _tournamentCachingService, _tableService);
	}

	[Fact]
	public async Task GetAuthCodeFromCacheAsync_ReturnsAuthCodeFromCache_WhenExists()
	{
		//Arrange
		string eventId = "25";
		string steamId = "1234";
		string key = $"TOURNAMENT_{eventId}_USER_{steamId}_AUTHCODE";

		string mockAuthCode = "MOCK_AUTH_CODE";

		_cache.TryGetValue(key, out string? _).Returns(x =>
		{
			x[1] = mockAuthCode;
			return true;
		});

		//Act
		var result = await _service.GetAuthCodeFromCacheAsync(eventId, steamId);

		//Assert
		Assert.Equal(mockAuthCode, result);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(key, out string? _);
		await _tableService.DidNotReceive().GetEntryIfExistsAsync(steamId, eventId);
	}

	[Fact]
	public async Task GetAuthCodeFromCacheAsync_CallsTableService_WhenDoesNotExistInCache()
	{
		//Arrange
		string eventId = "25";
		string steamId = "1234";
		string key = $"TOURNAMENT_{eventId}_USER_{steamId}_AUTHCODE";

		string mockAuthCode = "MOCK_AUTH_CODE";

		_cache.TryGetValue(key, out string? _).Returns(x =>
		{
			x[1] = null;
			return false;
		});

		UserEvent userEvent = new()
		{
			PartitionKey = eventId,
			RowKey = steamId,
			AuthCode = mockAuthCode
		};

		_tableService.GetEntryIfExistsAsync(steamId, eventId).Returns(userEvent);

		//Act
		var result = await _service.GetAuthCodeFromCacheAsync(eventId, steamId);

		//Assert
		Assert.Equal(mockAuthCode, result);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(key, out string? _);
		await _tableService.Received(Quantity.Exactly(1)).GetEntryIfExistsAsync(steamId, eventId);
		_cache.Received(Quantity.Exactly(1)).Set(key, mockAuthCode);
	}

	[Fact]
	public async Task GetAuthCodeFromCacheAsync_ThrowsException_WhenAuthCodeNotInCacheOrTable()
	{
		//Arrange
		string eventId = "25";
		string steamId = "1234";
		string key = $"TOURNAMENT_{eventId}_USER_{steamId}_AUTHCODE";

		_cache.TryGetValue(key, out string? _).Returns(x =>
		{
			x[1] = null;
			return false;
		});

		_tableService.GetEntryIfExistsAsync(steamId, eventId).Returns((UserEvent?)null);

		//Act & Assert
		var exception = await Assert.ThrowsAsync<Exception>(async () => await _service.GetAuthCodeFromCacheAsync(eventId, steamId));

		Assert.Equal($"Auth Code for the user {steamId} for event {eventId} is missing in the table storage.", exception.Message);
		await _tableService.Received(Quantity.Exactly(1)).GetEntryIfExistsAsync(steamId, eventId);
	}

	[Fact]
	public async Task CacheUserPredictionsAsync_GetsPredictionsFromCache_WhenExists()
	{
		//Arrange
		string eventId = "25";
		string steamId = "1234";
		string picksKey = $"USER_{steamId}_TOURNAMENT_{eventId}_PICKS";
		string mockAuthCode = "MOCK_AUTH_CODE";

		UserPredictions userPredictions = new UserPredictions()
		{
			Picks =
			[
				new()
				{
					GroupId = 1,
					Pick = 123,
					Index = 0
				}
			]
		};

		_cache.TryGetValue(picksKey, out UserPredictions? _).Returns(x =>
		{
			x[1] = userPredictions;
			return true;
		});

		//Act
		var result = await _service.CacheUserPredictionsAsync(steamId, eventId);


		//Assert
		Assert.Equal(userPredictions.Picks.Count, result.Picks.Count);
		Assert.Equal(userPredictions.Picks[0].Pick, result.Picks[0].Pick);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(picksKey, out UserPredictions? _);
		await _steamAPI.DidNotReceive().GetUserPredictionsAsync(steamId, eventId, mockAuthCode);
	}

	[Fact]
	public async Task CacheUserPredictionsAsync_CallsAPI_WhenDoesNotExistInCache()
	{
		//Arrange
		string eventId = "25";
		string steamId = "1234";
		string picksKey = $"USER_{steamId}_TOURNAMENT_{eventId}_PICKS";
		string authKey = $"TOURNAMENT_{eventId}_USER_{steamId}_AUTHCODE";
		string mockAuthCode = "MOCK_AUTH_CODE";

		UserPredictions userPredictions = new UserPredictions()
		{
			Picks =
			[
				new()
				{
					GroupId = 1,
					Pick = 123,
					Index = 0
				}
			]
		};

		_cache.TryGetValue(picksKey, out UserPredictions? _).Returns(x =>
		{
			x[1] = null;
			return false;
		});

		_cache.TryGetValue(authKey, out string? _).Returns(x =>
		{
			x[1] = mockAuthCode;
			return true;
		});

		_steamAPI.GetUserPredictionsAsync(steamId, eventId, mockAuthCode).Returns(new GetResult<UserPredictions> ()
		{
			Result = userPredictions
		});

		//Act
		var result = await _service.CacheUserPredictionsAsync(steamId, eventId);


		//Assert
		Assert.Equal(userPredictions.Picks.Count, result.Picks.Count);
		Assert.Equal(userPredictions.Picks[0].Pick, result.Picks[0].Pick);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(picksKey, out UserPredictions? _);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(authKey, out string? _);
		await _steamAPI.Received(Quantity.Exactly(1)).GetUserPredictionsAsync(steamId, eventId, mockAuthCode);
	}

	[Fact]
	public async Task CacheUserPredictionsAsync_ThrowsException_WhenAPIReturnsNull()
	{
		//Arrange
		string eventId = "25";
		string steamId = "1234";
		string authKey = $"TOURNAMENT_{eventId}_USER_{steamId}_AUTHCODE";
		string mockAuthCode = "MOCK_AUTH_CODE";

		_cache.TryGetValue(authKey, out string? _).Returns(x =>
		{
			x[1] = mockAuthCode;
			return true;
		});

		_steamAPI.GetUserPredictionsAsync(steamId, eventId, mockAuthCode).Returns(new GetResult<UserPredictions>()
		{
			Result = null!
		});

		//Act & Assert
		var exception = await Assert.ThrowsAsync<Exception>(async () => await _service.RefreshUserPredictionsAsync(steamId, eventId));

		Assert.Equal($"The user predictions from the Steam API for user {steamId}, for event {eventId} returned null", exception.Message);

		await _steamAPI.Received(Quantity.Exactly(1)).GetUserPredictionsAsync(steamId, eventId, mockAuthCode);
	}

	[Fact]
	public async Task RefreshUserPredictionsAsync_GetUserPredictionsFromAPI()
	{
		//Arrange
		string eventId = "25";
		string steamId = "1234";
		string picksKey = $"USER_{steamId}_TOURNAMENT_{eventId}_PICKS";
		string authKey = $"TOURNAMENT_{eventId}_USER_{steamId}_AUTHCODE";
		string mockAuthCode = "MOCK_AUTH_CODE";

		UserPredictions userPredictions = new UserPredictions()
		{
			Picks =
			[
				new()
				{
					GroupId = 1,
					Pick = 123,
					Index = 0
				}
			]
		};

		_cache.TryGetValue(authKey, out string? _).Returns(x =>
		{
			x[1] = mockAuthCode;
			return true;
		});

		_steamAPI.GetUserPredictionsAsync(steamId, eventId, mockAuthCode).Returns(new GetResult<UserPredictions>()
		{
			Result = userPredictions
		});

		//Act
		var result = await _service.RefreshUserPredictionsAsync(steamId, eventId);


		//Assert
		Assert.Equal(userPredictions.Picks.Count, result.Picks.Count);
		Assert.Equal(userPredictions.Picks[0].Pick, result.Picks[0].Pick);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(authKey, out string? _);
		await _steamAPI.Received(Quantity.Exactly(1)).GetUserPredictionsAsync(steamId, eventId, mockAuthCode);
		_cache.Received(Quantity.Exactly(1)).Set(picksKey, userPredictions, TimeSpan.FromMinutes(30));
	}

	[Fact]
	public async Task GetUserTeamsFromCacheAsync__ReturnsUserTeamsFromCache_WhenExists()
	{
		//Arrange
		string eventId = "25";
		string steamId = "1234";
		string key = $"USER_{steamId}_TOURNAMENT_{eventId}_TEAMS";
		string mockAuthCode = "MOCK_AUTH_CODE";

		List<Team> teams =
		[
			new ()
			{
				Logo = "logo.png",
				Name = "Team"
			}
		];

		_cache.TryGetValue(key, out IReadOnlyCollection<Team>? _).Returns(x =>
		{
			x[1] = teams;
			return true;
		});

		//Act
		var result = await _service.CacheUserTeamsAsync(steamId, eventId);

		//Assert
		Assert.Equal(teams.Count, result.Count);
		Assert.Equal(teams[0].Name, result.ToList()[0].Name);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(key, out IReadOnlyCollection<Team>? _);
		await _tournamentCachingService.DidNotReceive().GetTournamentTeamsAsync(eventId);
		await _steamAPI.DidNotReceive().GetTournamentItemsAsync(steamId, eventId, mockAuthCode);
	}

	[Fact]
	public async Task CacheUserTeamsAsync__CallsAPI_WhenDoesNotExistInCache()
	{
		//Arrange
		string eventId = "25";
		string steamId = "1234";
		string key = $"USER_{steamId}_TOURNAMENT_{eventId}_TEAMS";
		string authKey = $"TOURNAMENT_{eventId}_USER_{steamId}_AUTHCODE";
		string mockAuthCode = "MOCK_AUTH_CODE";

		List<Team> teams =
		[
			new ()
			{
				Logo = "logo.png",
				Name = "Team",
				TeamId = 1,
				PickId = 1
			}
		];

		TournamentItems items = new()
		{
			Items = teams
		};

		_cache.TryGetValue(key, out IReadOnlyCollection<Team>? _).Returns(x =>
		{
			x[1] = null;
			return false;
		});

		_cache.TryGetValue(authKey, out string? _).Returns(x =>
		{
			x[1] = mockAuthCode;
			return true;
		});

		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(teams);

		_steamAPI.GetTournamentItemsAsync(steamId, eventId, mockAuthCode).Returns(new GetResult<TournamentItems>() 
		{ 
			Result = items 
		});

		//Act
		var result = await _service.CacheUserTeamsAsync(steamId, eventId);

		//Assert
		Assert.Equal(teams.Count, result.Count);
		Assert.Equal(teams[0].Name, result.ToList()[0].Name);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(key, out IReadOnlyCollection<Team>? _);
		_cache.Received(Quantity.Exactly(1)).TryGetValue(authKey, out string? _);
		await _tournamentCachingService.Received(Quantity.Exactly(1)).GetTournamentTeamsAsync(eventId);
		await _steamAPI.Received(Quantity.Exactly(1)).GetTournamentItemsAsync(steamId, eventId, mockAuthCode);
		_cache.Received(Quantity.Exactly(1)).Set(key, teams, TimeSpan.FromMinutes(30));
	}
}