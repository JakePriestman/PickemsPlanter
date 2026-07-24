using NSubstitute;
using PickemsPlanter.Models.CoinProgress;
using PickemsPlanter.Models.Event;
using Xunit;

namespace PickemsPlanter.Services;

public class CoinProgressServiceTests
{
	private const string EventId = "25";
	private const string SteamId = "76561198000000000";
	private const string BlobContainerUrl = "https://sacs2.blob.core.windows.net/teamimages";

	private readonly CoinProgressService _service;
	private readonly IPickemsService _pickemsService = Substitute.For<IPickemsService>();

	public CoinProgressServiceTests()
	{
		_service = new(_pickemsService);

		// Baseline: nothing submitted anywhere, every stage/playoffs round still open — every
		// challenge except the always-true Coin Activation starts out incomplete. Individual
		// tests override only what they need to isolate a single challenge.
		foreach (Stages stage in new[] { Stages.Stage1, Stages.Stage2, Stages.Stage3 })
		{
			_pickemsService.GetStagePicksAsync(stage, SteamId, EventId).Returns([]);
			_pickemsService.GetStageResultsAsync(stage, EventId).Returns([]);
			_pickemsService.GetStagePicksAllowedAsync(stage, EventId).Returns(true);
		}

		_pickemsService.GetPlayoffPicksAsync(SteamId, EventId).Returns([]);
		_pickemsService.GetPlayoffResultsAsync(EventId).Returns([]);
		_pickemsService.GetPlayoffsPicksAllowedAsync(EventId).Returns(true);
	}

	private static List<string> Filled(params string[] logos) => [.. logos.Select(logo => $"{BlobContainerUrl}/{logo}.png")];

	private static List<string> Unknowns(int count) => [.. Enumerable.Repeat($"{BlobContainerUrl}/unknown.png", count)];

	[Fact]
	public async Task GetCoinProgressAsync_CoinActivationIsAlwaysCompleted()
	{
		// Act
		var result = await _service.GetCoinProgressAsync(SteamId, EventId);

		// Assert
		Assert.True(result.Challenges.Single(c => c.Name == "Coin Activation").Completed);
	}

	[Fact]
	public async Task GetCoinProgressAsync_DefaultsToBronze_WhenNothingSubmitted()
	{
		// Act
		var result = await _service.GetCoinProgressAsync(SteamId, EventId);

		// Assert — only Coin Activation is completed
		Assert.Equal(CoinTier.Bronze, result.Tier);
		Assert.Equal(1, result.CompletedChallenges);
		Assert.Equal(11, result.TotalChallenges);
	}

	[Fact]
	public async Task GetCoinProgressAsync_StageParticipation_RequiresAllTenPicks_AndStageLocked()
	{
		// Arrange
		List<string> tenPicks = Filled("a", "b", "c", "d", "e", "f", "g", "h", "i", "j");
		_pickemsService.GetStagePicksAsync(Stages.Stage1, SteamId, EventId).Returns(tenPicks);
		_pickemsService.GetStagePicksAllowedAsync(Stages.Stage1, EventId).Returns(true);

		// Act — stage still open, so participation can't be scored yet even though all 10 are filled
		var result = await _service.GetCoinProgressAsync(SteamId, EventId);

		// Assert
		Assert.False(result.Challenges.Single(c => c.Name == "Stage 1 Participation").Completed);
	}

	[Fact]
	public async Task GetCoinProgressAsync_StageParticipation_CompletesOnceLockedWithAllTenFilled()
	{
		// Arrange
		List<string> tenPicks = Filled("a", "b", "c", "d", "e", "f", "g", "h", "i", "j");
		_pickemsService.GetStagePicksAsync(Stages.Stage1, SteamId, EventId).Returns(tenPicks);
		_pickemsService.GetStagePicksAllowedAsync(Stages.Stage1, EventId).Returns(false);

		// Act
		var result = await _service.GetCoinProgressAsync(SteamId, EventId);

		// Assert
		Assert.True(result.Challenges.Single(c => c.Name == "Stage 1 Participation").Completed);
	}

	[Fact]
	public async Task GetCoinProgressAsync_StageParticipation_FailsWhenLockedButNotAllPicksMade()
	{
		// Arrange — locked, but only 8 of 10 slots filled (2 left as "unknown")
		List<string> picks = [.. Filled("a", "b", "c", "d", "e", "f", "g", "h"), .. Unknowns(2)];
		_pickemsService.GetStagePicksAsync(Stages.Stage1, SteamId, EventId).Returns(picks);
		_pickemsService.GetStagePicksAllowedAsync(Stages.Stage1, EventId).Returns(false);

		// Act
		var result = await _service.GetCoinProgressAsync(SteamId, EventId);

		// Assert
		Assert.False(result.Challenges.Single(c => c.Name == "Stage 1 Participation").Completed);
	}

	[Theory]
	[InlineData(5, true)]
	[InlineData(4, false)]
	public async Task GetCoinProgressAsync_StageAccuracy_RequiresAtLeastFiveCorrect(int correctCount, bool expectedCompleted)
	{
		// Arrange — first `correctCount` positions match the result, the rest are wrong picks
		List<string> results = Filled("a", "b", "c", "d", "e", "f", "g", "h", "i", "j");
		List<string> picks = [.. results.Take(correctCount), .. Filled("wrong1", "wrong2", "wrong3", "wrong4", "wrong5", "wrong6", "wrong7", "wrong8", "wrong9", "wrong10").Take(10 - correctCount)];

		_pickemsService.GetStagePicksAsync(Stages.Stage2, SteamId, EventId).Returns(picks);
		_pickemsService.GetStageResultsAsync(Stages.Stage2, EventId).Returns(results);

		// Act
		var result = await _service.GetCoinProgressAsync(SteamId, EventId);

		// Assert
		Assert.Equal(expectedCompleted, result.Challenges.Single(c => c.Name == "Stage 2 Accuracy").Completed);
	}

	[Fact]
	public async Task GetCoinProgressAsync_StageAccuracy_UndecidedResultsNeverCountAsCorrect()
	{
		// Arrange — user picked the same teams the results eventually show, but results are
		// still "unknown" (undecided) — must not be counted as a match against a real pick.
		List<string> picks = Filled("a", "b", "c", "d", "e", "f", "g", "h", "i", "j");
		List<string> results = Unknowns(10);

		_pickemsService.GetStagePicksAsync(Stages.Stage3, SteamId, EventId).Returns(picks);
		_pickemsService.GetStageResultsAsync(Stages.Stage3, EventId).Returns(results);

		// Act
		var result = await _service.GetCoinProgressAsync(SteamId, EventId);

		// Assert
		Assert.False(result.Challenges.Single(c => c.Name == "Stage 3 Accuracy").Completed);
	}

	[Fact]
	public async Task GetCoinProgressAsync_PlayoffsParticipation_CompletesOnceLockedWithAllSevenFilled()
	{
		// Arrange
		List<string> sevenPicks = Filled("a", "b", "c", "d", "e", "f", "g");
		_pickemsService.GetPlayoffPicksAsync(SteamId, EventId).Returns(sevenPicks);
		_pickemsService.GetPlayoffsPicksAllowedAsync(EventId).Returns(false);

		// Act
		var result = await _service.GetCoinProgressAsync(SteamId, EventId);

		// Assert
		Assert.True(result.Challenges.Single(c => c.Name == "Playoffs Participation").Completed);
	}

	[Theory]
	[InlineData(2, true)]
	[InlineData(1, false)]
	public async Task GetCoinProgressAsync_QuarterFinals_RequiresAtLeastTwoOfFourCorrect(int correctCount, bool expectedCompleted)
	{
		// Arrange — quarter-finals occupy positions 0-3 of the playoffs picks/results
		List<string> qfResults = Filled("a", "b", "c", "d");
		List<string> qfPicks = [.. qfResults.Take(correctCount), .. Filled("wrong1", "wrong2", "wrong3").Take(4 - correctCount)];
		List<string> picks = [.. qfPicks, .. Filled("e", "f"), .. Filled("champion")];
		List<string> results = [.. qfResults, .. Filled("e", "f"), .. Filled("champion")];

		_pickemsService.GetPlayoffPicksAsync(SteamId, EventId).Returns(picks);
		_pickemsService.GetPlayoffResultsAsync(EventId).Returns(results);

		// Act
		var result = await _service.GetCoinProgressAsync(SteamId, EventId);

		// Assert
		Assert.Equal(expectedCompleted, result.Challenges.Single(c => c.Name == "Quarter-Finals").Completed);
	}

	[Theory]
	[InlineData(1, true)]
	[InlineData(0, false)]
	public async Task GetCoinProgressAsync_SemiFinals_RequiresAtLeastOneOfTwoCorrect(int correctCount, bool expectedCompleted)
	{
		// Arrange — semi-finals occupy positions 4-5 of the playoffs picks/results
		List<string> sfResults = Filled("e", "f");
		List<string> sfPicks = [.. sfResults.Take(correctCount), .. Filled("wrong1").Take(2 - correctCount)];
		List<string> picks = [.. Filled("a", "b", "c", "d"), .. sfPicks, .. Filled("champion")];
		List<string> results = [.. Filled("a", "b", "c", "d"), .. sfResults, .. Filled("champion")];

		_pickemsService.GetPlayoffPicksAsync(SteamId, EventId).Returns(picks);
		_pickemsService.GetPlayoffResultsAsync(EventId).Returns(results);

		// Act
		var result = await _service.GetCoinProgressAsync(SteamId, EventId);

		// Assert
		Assert.Equal(expectedCompleted, result.Challenges.Single(c => c.Name == "Semi-Finals").Completed);
	}

	[Theory]
	[InlineData("champion", true)]
	[InlineData("someone-else", false)]
	public async Task GetCoinProgressAsync_GrandFinal_RequiresTheChampionPickToMatch(string championPick, bool expectedCompleted)
	{
		// Arrange — the champion occupies position 6 of the playoffs picks/results
		List<string> picks = [.. Filled("a", "b", "c", "d"), .. Filled("e", "f"), .. Filled(championPick)];
		List<string> results = [.. Filled("a", "b", "c", "d"), .. Filled("e", "f"), .. Filled("champion")];

		_pickemsService.GetPlayoffPicksAsync(SteamId, EventId).Returns(picks);
		_pickemsService.GetPlayoffResultsAsync(EventId).Returns(results);

		// Act
		var result = await _service.GetCoinProgressAsync(SteamId, EventId);

		// Assert
		Assert.Equal(expectedCompleted, result.Challenges.Single(c => c.Name == "Grand Final").Completed);
	}

	[Fact]
	public async Task GetCoinProgressAsync_ResolvesBronze_BelowFourCompletedChallenges()
	{
		// Arrange — Coin Activation + Stage 1 Participation only = 2 completed
		_pickemsService.GetStagePicksAsync(Stages.Stage1, SteamId, EventId).Returns(Filled("a", "b", "c", "d", "e", "f", "g", "h", "i", "j"));
		_pickemsService.GetStagePicksAllowedAsync(Stages.Stage1, EventId).Returns(false);

		// Act
		var result = await _service.GetCoinProgressAsync(SteamId, EventId);

		// Assert
		Assert.Equal(2, result.CompletedChallenges);
		Assert.Equal(CoinTier.Bronze, result.Tier);
	}

	[Fact]
	public async Task GetCoinProgressAsync_ResolvesSilver_AtFourCompletedChallenges()
	{
		// Arrange — Coin Activation + all 3 stage participations = 4
		foreach (var stage in new[] { Stages.Stage1, Stages.Stage2, Stages.Stage3 })
		{
			_pickemsService.GetStagePicksAsync(stage, SteamId, EventId).Returns(Filled("a", "b", "c", "d", "e", "f", "g", "h", "i", "j"));
			_pickemsService.GetStagePicksAllowedAsync(stage, EventId).Returns(false);
		}

		// Act
		var result = await _service.GetCoinProgressAsync(SteamId, EventId);

		// Assert
		Assert.Equal(4, result.CompletedChallenges);
		Assert.Equal(CoinTier.Silver, result.Tier);
	}

	[Fact]
	public async Task GetCoinProgressAsync_ResolvesGold_AtEightCompletedChallenges()
	{
		// Arrange — Coin Activation + 3 stage participations + 3 stage accuracies (picks ==
		// results, all 10 correct) + Playoffs Participation = 8
		foreach (var stage in new[] { Stages.Stage1, Stages.Stage2, Stages.Stage3 })
		{
			var stageResults = Filled("a", "b", "c", "d", "e", "f", "g", "h", "i", "j");
			_pickemsService.GetStagePicksAsync(stage, SteamId, EventId).Returns(stageResults);
			_pickemsService.GetStageResultsAsync(stage, EventId).Returns(stageResults);
			_pickemsService.GetStagePicksAllowedAsync(stage, EventId).Returns(false);
		}

		_pickemsService.GetPlayoffPicksAsync(SteamId, EventId).Returns(Filled("a", "b", "c", "d", "e", "f", "g"));
		_pickemsService.GetPlayoffsPicksAllowedAsync(EventId).Returns(false);

		// Act
		var result = await _service.GetCoinProgressAsync(SteamId, EventId);

		// Assert
		Assert.Equal(8, result.CompletedChallenges);
		Assert.Equal(CoinTier.Gold, result.Tier);
	}

	[Fact]
	public async Task GetCoinProgressAsync_ResolvesDiamond_WhenAllElevenChallengesCompleted()
	{
		// Arrange
		foreach (var stage in new[] { Stages.Stage1, Stages.Stage2, Stages.Stage3 })
		{
			var stageResults = Filled("a", "b", "c", "d", "e", "f", "g", "h", "i", "j");
			_pickemsService.GetStagePicksAsync(stage, SteamId, EventId).Returns(stageResults);
			_pickemsService.GetStageResultsAsync(stage, EventId).Returns(stageResults);
			_pickemsService.GetStagePicksAllowedAsync(stage, EventId).Returns(false);
		}

		var playoffs = Filled("a", "b", "c", "d", "e", "f", "champion");
		_pickemsService.GetPlayoffPicksAsync(SteamId, EventId).Returns(playoffs);
		_pickemsService.GetPlayoffResultsAsync(EventId).Returns(playoffs);
		_pickemsService.GetPlayoffsPicksAllowedAsync(EventId).Returns(false);

		// Act
		var result = await _service.GetCoinProgressAsync(SteamId, EventId);

		// Assert
		Assert.Equal(11, result.CompletedChallenges);
		Assert.Equal(CoinTier.Diamond, result.Tier);
	}
}
