using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PickemsPlanter.Models.Admin;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.Simulator;
using PickemsPlanter.Models.Steam;
using PickemsPlanter.Models.StorageAccount;
using PickemsPlanter.Services;
using System.Security.Claims;
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
	private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
	private readonly IAdvancingSeedAutomationService _advancingSeedAutomationService = Substitute.For<IAdvancingSeedAutomationService>();
	private readonly IPandaScoreResultsService _pandaScoreResultsService = Substitute.For<IPandaScoreResultsService>();

	private const string EventId = "25";

	public SeedingModelTests()
	{
		var httpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity(
			[
				new Claim("PersonaName", "TestAdmin"),
				new Claim("Avatar", "avatar.png")
			]))
		};
		_httpContextAccessor.HttpContext.Returns(httpContext);

		_model = new(_eventTableService, _seedsTableService, _stageRosterService, _hltvRankingParser, _httpContextAccessor,
			_advancingSeedAutomationService, _pandaScoreResultsService, NullLogger<SeedingModel>.Instance)
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
		_model.AppliedStages = [Stages.Stage2];

		// Act
		await _model.OnGetAsync();

		// Assert
		Assert.Equal("Stage 2 invites seeds saved.", _model.StatusMessage);
		Assert.True(_model.StatusIsSuccess);
	}

	[Fact]
	public async Task OnGetAsync_ShowsCombinedAppliedMessage_WhenMultipleStagesWereSaved()
	{
		// Arrange
		_model.Applied = true;
		_model.AppliedStages = [Stages.Stage1, Stages.Stage2];

		// Act
		await _model.OnGetAsync();

		// Assert
		Assert.Equal("Stage 1, Stage 2 invites seeds saved.", _model.StatusMessage);
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
		Assert.Equal(16, _model.Previews[Stages.Stage1].Rows.Count);
		Assert.Equal(8, _model.Previews[Stages.Stage2].Rows.Count);
		Assert.Equal(8, _model.Previews[Stages.Stage3].Rows.Count);
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
			.Returns((IReadOnlyList<Team>?)null);

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
	public async Task OnPostUploadAsync_StageWithInflatedRoster_ShowsOnlyTheConfirmedEightInvites()
	{
		// Arrange — Steam reports 24 for a Stage 2/3 whose previous stage hasn't concluded yet
		// (that stage's full 16-team candidate pool plus this stage's own 8 confirmed
		// invites). The confirmed invites are still knowable — always the first 8 in Steam's
		// own order, per StageRosterService — so the preview should show exactly those 8, not
		// all 24 candidates.
		List<Team> confirmedInvites = [.. Enumerable.Range(1, 8).Select(i => new Team { PickId = 100 + i, Name = $"Invite {i}", Logo = $"invite{i}" })];

		RosterReturns(Stages.Stage1, SixteenTeams());
		RosterReturns(Stages.Stage2, [.. confirmedInvites, .. SixteenTeams()]);
		RosterReturns(Stages.Stage3, SixteenTeams());

		_stageRosterService.GetLikelyInviteTeamsAsync(EventId, Stages.Stage2).Returns(confirmedInvites);
		_stageRosterService.GetLikelyInviteTeamsAsync(EventId, Stages.Stage3).Returns((IReadOnlyList<Team>?)null);

		List<HltvRankedTeam> hltvTeams = [.. Enumerable.Range(1, 8).Select(i => new HltvRankedTeam(i, $"Invite {i}"))];
		ParserReturns(hltvTeams);

		// Act
		await _model.OnPostUploadAsync(FakeFile());

		// Assert
		var stage2 = _model.Previews[Stages.Stage2];
		Assert.Equal(8, stage2.Rows.Count);
		Assert.All(stage2.Rows, r => Assert.True(r.Matched));
		Assert.True(stage2.CanApply);
	}

	[Fact]
	public async Task OnPostUploadAsync_StageWithInflatedRoster_ExplainsThePreviousStageHasNotConcluded_WhenConfirmedInvitesUnavailable()
	{
		// Arrange — same inflated-roster scenario, but the confirmed invites genuinely
		// couldn't be determined (defensive fallback — GetLikelyInviteTeamsAsync always
		// resolves this deterministically in practice, since it's just a Take(8)).
		RosterReturns(Stages.Stage1, SixteenTeams());
		RosterReturns(Stages.Stage2, [.. SixteenTeams(), .. Enumerable.Range(101, 8).Select(i => Team(i))]);
		RosterReturns(Stages.Stage3, SixteenTeams());
		ParserReturns(AllSixteenMatch());

		_stageRosterService.GetLikelyInviteTeamsAsync(EventId, Arg.Any<Stages>()).Returns((IReadOnlyList<Team>?)null);

		// Act
		await _model.OnPostUploadAsync(FakeFile());

		// Assert
		Assert.False(_model.Previews[Stages.Stage2].CanApply);
		Assert.Contains("couldn't be determined yet", _model.Previews[Stages.Stage2].Message);
	}

	[Fact]
	public async Task OnPostUploadAsync_Stage2_ShowsOnlyConfirmedInvites_IgnoringAdvancers()
	{
		// Arrange — likely invites are teams I-P (pickids 9-16); the other 8 (advancers) are
		// deliberately left unmatched in the uploaded ranking, which must NOT block Apply
		// since advancers are never part of the Stage 2 preview at all.
		var roster = SixteenTeams();
		RosterReturns(Stages.Stage1, SixteenTeams());
		RosterReturns(Stages.Stage2, roster);
		RosterReturns(Stages.Stage3, SixteenTeams());

		_stageRosterService.GetLikelyInviteTeamsAsync(EventId, Stages.Stage2)
			.Returns(roster.Where(t => t.PickId > 8).ToList());
		_stageRosterService.GetLikelyInviteTeamsAsync(EventId, Stages.Stage3)
			.Returns((IReadOnlyList<Team>?)null);

		List<HltvRankedTeam> hltvTeams = [.. Enumerable.Range(9, 8).Select(i => new HltvRankedTeam(i, TeamName(i)))];
		ParserReturns(hltvTeams);

		// Act
		await _model.OnPostUploadAsync(FakeFile());

		// Assert
		var stage2 = _model.Previews[Stages.Stage2];
		Assert.True(stage2.CanApply);
		Assert.Equal(8, stage2.Rows.Count);
		Assert.All(stage2.Rows, r => Assert.True(r.Matched));
		Assert.DoesNotContain(stage2.Rows, r => r.TeamName == TeamName(1)); // an advancer, not an invite
	}

	private static StageSelection Selection(Stages stage, bool selected, List<SeedPreviewRow> rows) =>
		new() { Stage = stage, Selected = selected, Rows = rows };

	[Fact]
	public async Task OnPostApplyAsync_Stage1_WritesAllSixteenSeeds_ReIndexedByRelativeOrder()
	{
		// Arrange — global HLTV ranks (not 1-16) must be re-indexed to seeds 1-16 by relative order.
		_model.Selections =
		[
			Selection(Stages.Stage1, selected: true,
			[
				new() { TeamName = "Team A", Rank = 200, Matched = true },
				new() { TeamName = "Team B", Rank = 50, Matched = true },
				.. Enumerable.Range(1, 14).Select(i => new SeedPreviewRow
				{
					TeamName = $"Filler {i}",
					Rank = 1000 + i,
					Matched = true
				})
			])
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
	public async Task OnPostApplyAsync_Stage2_WritesTheEightInviteSeeds()
	{
		// Arrange
		_model.Selections = [Selection(Stages.Stage2, selected: true,
			[.. Enumerable.Range(1, 8).Select(i => new SeedPreviewRow { TeamName = $"Invite {i}", Rank = i, Matched = true })])];

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
	public async Task OnPostApplyAsync_RedirectsWithTheAppliedStages()
	{
		// Arrange
		_model.Selections = [Selection(Stages.Stage3, selected: true,
			[.. Enumerable.Range(1, 8).Select(i => new SeedPreviewRow { TeamName = $"Invite {i}", Rank = i, Matched = true })])];

		// Act
		var result = await _model.OnPostApplyAsync();

		// Assert
		var redirect = Assert.IsType<RedirectToPageResult>(result);
		Assert.Equal(new List<Stages> { Stages.Stage3 }, redirect.RouteValues!["AppliedStages"]);
	}

	[Fact]
	public async Task OnPostApplyAsync_SavesEveryCheckedStage_InOneSubmit()
	{
		// Arrange — checking multiple stages at once should write all of them in a single Apply.
		_model.Selections =
		[
			Selection(Stages.Stage1, selected: true, [.. Enumerable.Range(1, 16).Select(i => new SeedPreviewRow { TeamName = $"Team {i}", Rank = i, Matched = true })]),
			Selection(Stages.Stage2, selected: true, [.. Enumerable.Range(1, 8).Select(i => new SeedPreviewRow { TeamName = $"Invite {i}", Rank = i, Matched = true })]),
			Selection(Stages.Stage3, selected: false, [.. Enumerable.Range(1, 8).Select(i => new SeedPreviewRow { TeamName = $"Invite {i}", Rank = i, Matched = true })])
		];

		// Act
		var result = await _model.OnPostApplyAsync();

		// Assert
		Assert.IsType<RedirectToPageResult>(result);
		await _seedsTableService.Received(1).UpsertSeedsAsync(Stages.Stage1, EventId, Arg.Any<IReadOnlyDictionary<string, int>>());
		await _seedsTableService.Received(1).UpsertSeedsAsync(Stages.Stage2, EventId, Arg.Any<IReadOnlyDictionary<string, int>>());
		await _seedsTableService.DidNotReceive().UpsertSeedsAsync(Stages.Stage3, EventId, Arg.Any<IReadOnlyDictionary<string, int>>());
	}

	[Fact]
	public async Task OnPostApplyAsync_ReTriggersAdvancingSeedAutomation_ForEveryAppliedStage()
	{
		// Arrange — a stage's own seeds are the Buchholz tiebreak input for the NEXT stage's
		// 9-16 (AdvancingSeedAutomationService), which otherwise only recomputes on
		// PandaScoreResultsCachingService's next poll tick. Apply should re-trigger it
		// immediately so the next stage's advancer order doesn't stay stale in the meantime.
		_model.Selections =
		[
			Selection(Stages.Stage1, selected: true, [.. Enumerable.Range(1, 16).Select(i => new SeedPreviewRow { TeamName = $"Team {i}", Rank = i, Matched = true })]),
			Selection(Stages.Stage2, selected: false, [.. Enumerable.Range(1, 8).Select(i => new SeedPreviewRow { TeamName = $"Invite {i}", Rank = i, Matched = true })])
		];

		// Act
		await _model.OnPostApplyAsync();

		// Assert
		await _pandaScoreResultsService.Received(1).GetCompletedMatchesAsync(EventId, Stages.Stage1);
		await _advancingSeedAutomationService.Received(1).ApplyAdvancingSeedsAsync(EventId, Stages.Stage1, Arg.Any<IReadOnlyCollection<SimulatorMatchResult>>());
		await _advancingSeedAutomationService.DidNotReceive().ApplyAdvancingSeedsAsync(EventId, Stages.Stage2, Arg.Any<IReadOnlyCollection<SimulatorMatchResult>>());
	}

	[Fact]
	public async Task OnPostApplyAsync_StillSavesAndRedirects_WhenTheAdvancingSeedReTriggerThrows()
	{
		// Arrange — the re-trigger is a best-effort freshness improvement, not core to Apply's
		// success: the seeds are already durably written by this point, so a failure here must
		// never turn a successful save into an error.
		_advancingSeedAutomationService.ApplyAdvancingSeedsAsync(Arg.Any<string>(), Arg.Any<Stages>(), Arg.Any<IReadOnlyCollection<SimulatorMatchResult>>())
			.ThrowsAsync(new InvalidOperationException("boom"));

		_model.Selections = [Selection(Stages.Stage1, selected: true,
			[.. Enumerable.Range(1, 16).Select(i => new SeedPreviewRow { TeamName = $"Team {i}", Rank = i, Matched = true })])];

		// Act
		var result = await _model.OnPostApplyAsync();

		// Assert
		Assert.IsType<RedirectToPageResult>(result);
		await _seedsTableService.Received(1).UpsertSeedsAsync(Stages.Stage1, EventId, Arg.Any<IReadOnlyDictionary<string, int>>());
	}

	[Fact]
	public async Task OnPostApplyAsync_WritesNothing_WhenNoStageIsChecked()
	{
		// Arrange
		_model.Selections = [Selection(Stages.Stage1, selected: false,
			[.. Enumerable.Range(1, 16).Select(i => new SeedPreviewRow { TeamName = $"Team {i}", Rank = i, Matched = true })])];

		// Act
		var result = await _model.OnPostApplyAsync();

		// Assert
		Assert.IsType<PageResult>(result);
		Assert.NotNull(_model.StatusMessage);
		await _seedsTableService.DidNotReceiveWithAnyArgs().UpsertSeedsAsync(default, default!, default!);
	}

	[Fact]
	public async Task OnPostApplyAsync_WritesNothing_WhenOneCheckedStageIsInvalid_EvenIfAnotherCheckedStageIsValid()
	{
		// Arrange — atomic across the checked set: one bad stage blocks the whole submit.
		_model.Selections =
		[
			Selection(Stages.Stage1, selected: true, [.. Enumerable.Range(1, 16).Select(i => new SeedPreviewRow { TeamName = $"Team {i}", Rank = i, Matched = true })]),
			Selection(Stages.Stage2, selected: true, [.. Enumerable.Range(1, 9).Select(i => new SeedPreviewRow { TeamName = $"Team {i}", Rank = i, Matched = true })])
		];

		// Act
		var result = await _model.OnPostApplyAsync();

		// Assert
		Assert.IsType<PageResult>(result);
		await _seedsTableService.DidNotReceiveWithAnyArgs().UpsertSeedsAsync(default, default!, default!);
	}

	[Fact]
	public async Task OnPostApplyAsync_DoesNotApply_WhenRowCountIsWrong()
	{
		// Arrange — Stage 2/3 must always be exactly 8 rows.
		_model.Selections = [Selection(Stages.Stage2, selected: true,
			[.. Enumerable.Range(1, 9).Select(i => new SeedPreviewRow { TeamName = $"Team {i}", Rank = i, Matched = true })])];

		// Act
		var result = await _model.OnPostApplyAsync();

		// Assert
		Assert.IsType<PageResult>(result);
		Assert.False(_model.Previews[Stages.Stage2].CanApply);
		await _seedsTableService.DidNotReceiveWithAnyArgs().UpsertSeedsAsync(default, default!, default!);
	}

	[Fact]
	public async Task OnPostApplyAsync_DoesNotApply_WhenAnyRowIsUnmatched()
	{
		// Arrange
		_model.Selections =
		[
			Selection(Stages.Stage1, selected: true,
			[
				new() { TeamName = "Team A", Rank = null, Matched = false },
				.. Enumerable.Range(1, 15).Select(i => new SeedPreviewRow { TeamName = $"Team {i}", Rank = i, Matched = true })
			])
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
