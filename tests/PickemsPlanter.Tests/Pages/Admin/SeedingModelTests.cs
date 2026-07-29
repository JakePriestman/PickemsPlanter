using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
using PickemsPlanter.Models.Admin;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.Steam;
using PickemsPlanter.Models.StorageAccount;
using PickemsPlanter.Services;
using Xunit;
using TournamentEvent = PickemsPlanter.Models.Event.Event;

namespace PickemsPlanter.Pages.Admin;

public class SeedingModelTests
{
	private readonly SeedingModel _model;
	private readonly IEventTableService _eventTableService = Substitute.For<IEventTableService>();
	private readonly ISeedsTableService _seedsTableService = Substitute.For<ISeedsTableService>();
	private readonly IStageRosterService _stageRosterService = Substitute.For<IStageRosterService>();
	private readonly IHltvRankingParser _hltvRankingParser = Substitute.For<IHltvRankingParser>();

	private const string EventId = "25";

	public SeedingModelTests()
	{
		_model = new(_eventTableService, _seedsTableService, _stageRosterService, _hltvRankingParser)
		{
			EventId = EventId
		};

		_eventTableService.GetAllEventsAsync().Returns((IReadOnlyCollection<TournamentEvent>)[]);
		_seedsTableService.GetSeedsInStageAsync(Arg.Any<Stages>(), Arg.Any<string>()).Returns((IReadOnlyCollection<Seed>)[]);
	}

	// Letter-based names (not "Team 1".."Team 16") deliberately avoid numeric-suffix collisions
	// that would otherwise trip the normalized-substring fuzzy-match fallback (eg. "team16"
	// contains "team1").
	private static string TeamName(int pickId) => $"Team {(char)('A' + pickId - 1)}";

	private static Team Team(int pickId, string? name = null) => new() { PickId = pickId, Name = name ?? TeamName(pickId), Logo = (name ?? TeamName(pickId)).Replace(" ", "").ToLowerInvariant() };

	private static List<Team> SixteenTeams() => [.. Enumerable.Range(1, 16).Select(i => Team(i))];

	private static IFormFile FakeFile()
	{
		var file = Substitute.For<IFormFile>();
		file.Length.Returns(1024);
		file.OpenReadStream().Returns(new MemoryStream());
		return file;
	}

	private void RosterReturns(Stages stage, List<Team> roster) =>
		_stageRosterService.GetStageRosterAsync(EventId, stage).Returns(roster);

	private void AllStagesHaveFullRosters()
	{
		foreach (var stage in SeedingModel.SeedableStages)
		{
			var roster = SixteenTeams();
			RosterReturns(stage, roster);

			if (SeedingModel.IsInviteOnlyStage(stage))
				_stageRosterService.GetLikelyInviteTeamsAsync(EventId, stage).Returns(roster.Where(t => t.PickId > 8).ToList());
		}
	}

	private void ParserReturns(IReadOnlyList<HltvRankedTeam> teams) =>
		_hltvRankingParser.Parse(Arg.Any<Stream>()).Returns(teams);

	private static List<HltvRankedTeam> AllSixteenMatch() =>
		[.. Enumerable.Range(1, 16).Select(i => new HltvRankedTeam(i, TeamName(i)))];

	[Fact]
	public async Task OnGetAsync_OnlyListsActiveEvents()
	{
		// Arrange
		_eventTableService.GetAllEventsAsync().Returns((IReadOnlyCollection<TournamentEvent>)
		[
			new() { Id = "25", Name = "Active Event", Disabled = false },
			new() { Id = "26", Name = "Disabled Event", Disabled = true }
		]);

		// Act
		await _model.OnGetAsync();

		// Assert
		Assert.Single(_model.Events);
		Assert.Equal("25", _model.Events.Single().Id);
	}

	[Fact]
	public async Task OnGetAsync_LoadsCurrentSeeds_ForAllThreeStages()
	{
		// Act
		await _model.OnGetAsync();

		// Assert
		Assert.Equal(3, _model.CurrentSeedsByStage.Count);
		Assert.Contains(Stages.Stage1, _model.CurrentSeedsByStage.Keys);
		Assert.Contains(Stages.Stage2, _model.CurrentSeedsByStage.Keys);
		Assert.Contains(Stages.Stage3, _model.CurrentSeedsByStage.Keys);
	}

	[Fact]
	public async Task OnGetAsync_ShowsStageSpecificAppliedMessage()
	{
		// Arrange
		_model.Applied = true;
		_model.AppliedStage = Stages.Stage2;

		// Act
		await _model.OnGetAsync();

		// Assert
		Assert.Equal("Stage 2 invites seeds applied.", _model.StatusMessage);
		Assert.True(_model.StatusIsSuccess);
	}

	[Fact]
	public async Task OnPostUploadAsync_SetsStatusMessage_WhenEventMissing()
	{
		// Arrange
		_model.EventId = null;

		// Act
		await _model.OnPostUploadAsync(FakeFile());

		// Assert
		Assert.NotNull(_model.StatusMessage);
		Assert.False(_model.HasPreview);
		await _stageRosterService.DidNotReceiveWithAnyArgs().GetStageRosterAsync(default!, default);
	}

	[Fact]
	public async Task OnPostUploadAsync_SetsStatusMessage_WhenFileIsMissing()
	{
		// Act
		await _model.OnPostUploadAsync(null);

		// Assert
		Assert.NotNull(_model.StatusMessage);
		Assert.False(_model.HasPreview);
	}

	[Fact]
	public async Task OnPostUploadAsync_SetsStatusMessage_WhenNoTeamsParsed()
	{
		// Arrange
		ParserReturns([]);

		// Act
		await _model.OnPostUploadAsync(FakeFile());

		// Assert
		Assert.NotNull(_model.StatusMessage);
		Assert.False(_model.HasPreview);
	}

	[Fact]
	public async Task OnPostUploadAsync_BuildsPreviews_ForAllThreeStages_InOneUpload()
	{
		// Arrange
		AllStagesHaveFullRosters();
		ParserReturns(AllSixteenMatch());

		// Act
		await _model.OnPostUploadAsync(FakeFile());

		// Assert
		Assert.True(_model.HasPreview);
		Assert.Equal(3, _model.Previews.Count);
		Assert.All(SeedingModel.SeedableStages, stage => Assert.True(_model.Previews[stage].CanApply));
		_hltvRankingParser.Received(1).Parse(Arg.Any<Stream>());
	}

	[Fact]
	public async Task OnPostUploadAsync_Stage1_OrdersPreviewByHltvRank()
	{
		// Arrange
		AllStagesHaveFullRosters();

		// Reverse HLTV rank order vs team lettering, to prove the preview is sorted by rank.
		List<HltvRankedTeam> hltvTeams = [.. Enumerable.Range(1, 16).Select(i => new HltvRankedTeam(i, TeamName(17 - i)))];
		ParserReturns(hltvTeams);

		// Act
		await _model.OnPostUploadAsync(FakeFile());

		// Assert
		var stage1 = _model.Previews[Stages.Stage1];
		Assert.Equal(TeamName(16), stage1.Rows[0].TeamName); // rank 1
		Assert.Equal(TeamName(1), stage1.Rows[15].TeamName); // rank 16
	}

	[Fact]
	public async Task OnPostUploadAsync_MarksStagePreviewUnableToApply_WhenATeamIsUnmatched_ButOtherStagesAreUnaffected()
	{
		// Arrange — Stage1's roster has one extra, unmatchable team name; Stage2/3 fully match.
		RosterReturns(Stages.Stage1, [.. SixteenTeams().Take(15), Team(99, "Unmatchable FC")]);
		RosterReturns(Stages.Stage2, SixteenTeams());
		RosterReturns(Stages.Stage3, SixteenTeams());
		ParserReturns(AllSixteenMatch());

		_stageRosterService.GetLikelyInviteTeamsAsync(EventId, Arg.Any<Stages>())
			.Returns((IReadOnlyCollection<Team>?)null);

		// Act
		await _model.OnPostUploadAsync(FakeFile());

		// Assert
		Assert.False(_model.Previews[Stages.Stage1].CanApply);
		Assert.Contains("Unmatchable FC", _model.Previews[Stages.Stage1].Message);
	}

	[Fact]
	public async Task OnPostUploadAsync_StageWithIncompleteRoster_DoesNotAffectOtherStages()
	{
		// Arrange
		RosterReturns(Stages.Stage1, SixteenTeams());
		RosterReturns(Stages.Stage2, SixteenTeams().Take(10).ToList());
		RosterReturns(Stages.Stage3, SixteenTeams());
		ParserReturns(AllSixteenMatch());

		// Act
		await _model.OnPostUploadAsync(FakeFile());

		// Assert
		Assert.True(_model.Previews[Stages.Stage1].CanApply);
		Assert.False(_model.Previews[Stages.Stage2].CanApply);
		Assert.Contains("roster isn't fully known yet", _model.Previews[Stages.Stage2].Message);
	}

	[Fact]
	public async Task OnPostUploadAsync_Stage2_PreChecksLikelyInvites()
	{
		// Arrange — likely invites are teams I-P (pickids 9-16); the other 8 (advancers) are
		// deliberately left unmatched, which must NOT block Apply since they aren't selected.
		var roster = SixteenTeams();
		RosterReturns(Stages.Stage1, SixteenTeams());
		RosterReturns(Stages.Stage2, roster);
		RosterReturns(Stages.Stage3, SixteenTeams());

		_stageRosterService.GetLikelyInviteTeamsAsync(EventId, Stages.Stage2)
			.Returns(roster.Where(t => t.PickId > 8).ToList());
		_stageRosterService.GetLikelyInviteTeamsAsync(EventId, Stages.Stage3)
			.Returns((IReadOnlyCollection<Team>?)null);

		List<HltvRankedTeam> hltvTeams = [.. Enumerable.Range(9, 8).Select(i => new HltvRankedTeam(i, TeamName(i)))];
		ParserReturns(hltvTeams);

		// Act
		await _model.OnPostUploadAsync(FakeFile());

		// Assert
		var stage2 = _model.Previews[Stages.Stage2];
		Assert.True(stage2.CanApply);
		Assert.Equal(8, stage2.Rows.Count(r => r.Selected));
		Assert.DoesNotContain(stage2.Rows, r => r.Selected && !r.Matched);
	}

	[Fact]
	public async Task OnPostApplyAsync_Stage1_WritesAllSixteenSeeds_ReIndexedByRelativeOrder()
	{
		// Arrange — global HLTV ranks (not 1-16) must be re-indexed to seeds 1-16 by relative order.
		_model.Stage = Stages.Stage1;
		_model.PreviewRows =
		[
			new() { TeamName = "Team A", Rank = 200, Matched = true, Selected = true },
			new() { TeamName = "Team B", Rank = 50, Matched = true, Selected = true },
			.. Enumerable.Range(1, 14).Select(i => new SeedPreviewRow
			{
				TeamName = $"Filler {i}",
				Rank = 1000 + i,
				Matched = true,
				Selected = true
			})
		];

		// Act
		var result = await _model.OnPostApplyAsync();

		// Assert
		Assert.IsType<RedirectToPageResult>(result);
		await _seedsTableService.Received(1).UpsertSeedsAsync(
			Stages.Stage1,
			EventId,
			Arg.Is<IReadOnlyDictionary<string, int>>(d => d.Count == 16 && d["Team B"] == 1 && d["Team A"] == 2 && d["Filler 1"] == 3));
	}

	[Fact]
	public async Task OnPostApplyAsync_Stage2_WritesOnlySelectedEightSeeds()
	{
		// Arrange
		_model.Stage = Stages.Stage2;
		_model.PreviewRows =
		[
			.. Enumerable.Range(1, 8).Select(i => new SeedPreviewRow { TeamName = $"Invite {i}", Rank = i, Matched = true, Selected = true }),
			.. Enumerable.Range(1, 8).Select(i => new SeedPreviewRow { TeamName = $"Advancer {i}", Rank = null, Matched = false, Selected = false })
		];

		// Act
		var result = await _model.OnPostApplyAsync();

		// Assert
		Assert.IsType<RedirectToPageResult>(result);
		await _seedsTableService.Received(1).UpsertSeedsAsync(
			Stages.Stage2,
			EventId,
			Arg.Is<IReadOnlyDictionary<string, int>>(d => d.Count == 8 && d.Keys.All(k => k.StartsWith("Invite"))));
	}

	[Fact]
	public async Task OnPostApplyAsync_RedirectsWithTheAppliedStage()
	{
		// Arrange
		_model.Stage = Stages.Stage3;
		_model.PreviewRows = [.. Enumerable.Range(1, 8).Select(i => new SeedPreviewRow { TeamName = $"Invite {i}", Rank = i, Matched = true, Selected = true })];

		// Act
		var result = await _model.OnPostApplyAsync();

		// Assert
		var redirect = Assert.IsType<RedirectToPageResult>(result);
		Assert.Equal(Stages.Stage3, redirect.RouteValues!["AppliedStage"]);
	}

	[Fact]
	public async Task OnPostApplyAsync_DoesNotApply_WhenMoreThanExpectedCountSelected()
	{
		// Arrange
		_model.Stage = Stages.Stage2;
		_model.PreviewRows = [.. Enumerable.Range(1, 9).Select(i => new SeedPreviewRow { TeamName = $"Team {i}", Rank = i, Matched = true, Selected = true })];

		// Act
		var result = await _model.OnPostApplyAsync();

		// Assert
		Assert.IsType<PageResult>(result);
		Assert.False(_model.Previews[Stages.Stage2].CanApply);
		await _seedsTableService.DidNotReceiveWithAnyArgs().UpsertSeedsAsync(default, default!, default!);
	}

	[Fact]
	public async Task OnPostApplyAsync_DoesNotApply_WhenAnyRelevantRowIsUnmatched()
	{
		// Arrange
		_model.Stage = Stages.Stage1;
		_model.PreviewRows =
		[
			new() { TeamName = "Team A", Rank = null, Matched = false, Selected = true },
			.. Enumerable.Range(1, 15).Select(i => new SeedPreviewRow { TeamName = $"Team {i}", Rank = i, Matched = true, Selected = true })
		];

		// Act
		var result = await _model.OnPostApplyAsync();

		// Assert
		Assert.IsType<PageResult>(result);
		Assert.Contains("Team A", _model.Previews[Stages.Stage1].Message);
		await _seedsTableService.DidNotReceiveWithAnyArgs().UpsertSeedsAsync(default, default!, default!);
	}

	[Fact]
	public async Task OnPostApplyAsync_SetsStatusMessage_WhenEventMissing()
	{
		// Arrange
		_model.EventId = null;

		// Act
		var result = await _model.OnPostApplyAsync();

		// Assert
		Assert.IsType<PageResult>(result);
		Assert.NotNull(_model.StatusMessage);
		await _seedsTableService.DidNotReceiveWithAnyArgs().UpsertSeedsAsync(default, default!, default!);
	}
}
