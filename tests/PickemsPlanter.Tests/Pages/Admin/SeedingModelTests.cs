using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
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

	[Fact]
	public async Task OnPostUploadAsync_SetsStatusMessage_WhenEventOrStageMissing()
	{
		// Arrange
		_model.EventId = null;
		_model.Stage = Stages.Stage1;

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
		// Arrange
		_model.Stage = Stages.Stage1;

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
		_model.Stage = Stages.Stage1;
		_hltvRankingParser.Parse(Arg.Any<Stream>()).Returns((IReadOnlyList<HltvRankedTeam>)[]);

		// Act
		await _model.OnPostUploadAsync(FakeFile());

		// Assert
		Assert.NotNull(_model.StatusMessage);
		Assert.False(_model.HasPreview);
	}

	[Fact]
	public async Task OnPostUploadAsync_SetsStatusMessage_WhenRosterIsNot16Teams()
	{
		// Arrange
		_model.Stage = Stages.Stage1;
		_hltvRankingParser.Parse(Arg.Any<Stream>()).Returns([new HltvRankedTeam(1, "Team 1")]);
		_stageRosterService.GetStageRosterAsync(EventId, Stages.Stage1).Returns(SixteenTeams().Take(10).ToList());

		// Act
		await _model.OnPostUploadAsync(FakeFile());

		// Assert
		Assert.NotNull(_model.StatusMessage);
		Assert.False(_model.HasPreview);
	}

	[Fact]
	public async Task OnPostUploadAsync_Stage1_BuildsFullPreview_OrderedByHltvRank_WhenAllMatch()
	{
		// Arrange
		_model.Stage = Stages.Stage1;
		_stageRosterService.GetStageRosterAsync(EventId, Stages.Stage1).Returns(SixteenTeams());

		// Reverse HLTV rank order vs team numbering, to prove the preview is sorted by rank.
		List<HltvRankedTeam> hltvTeams = [.. Enumerable.Range(1, 16).Select(i => new HltvRankedTeam(i, TeamName(17 - i)))];
		_hltvRankingParser.Parse(Arg.Any<Stream>()).Returns(hltvTeams);

		// Act
		await _model.OnPostUploadAsync(FakeFile());

		// Assert
		Assert.True(_model.HasPreview);
		Assert.True(_model.CanApply);
		Assert.Equal(16, _model.PreviewRows.Count);
		Assert.All(_model.PreviewRows, r => Assert.True(r.Matched));
		Assert.Equal(TeamName(16), _model.PreviewRows[0].TeamName); // rank 1
		Assert.Equal(TeamName(1), _model.PreviewRows[15].TeamName); // rank 16
	}

	[Fact]
	public async Task OnPostUploadAsync_Stage1_CannotApply_WhenATeamIsUnmatched()
	{
		// Arrange
		_model.Stage = Stages.Stage1;
		_stageRosterService.GetStageRosterAsync(EventId, Stages.Stage1).Returns(SixteenTeams());

		List<HltvRankedTeam> hltvTeams = [.. Enumerable.Range(1, 15).Select(i => new HltvRankedTeam(i, TeamName(i)))];
		_hltvRankingParser.Parse(Arg.Any<Stream>()).Returns(hltvTeams);

		// Act
		await _model.OnPostUploadAsync(FakeFile());

		// Assert
		Assert.False(_model.CanApply);
		Assert.Contains(TeamName(16), _model.StatusMessage);
	}

	[Fact]
	public async Task OnPostUploadAsync_Stage2_PreChecksLikelyInvites_AndIgnoresAdvancersForMatchCompleteness()
	{
		// Arrange — likely invites are teams 9-16; team 1 (an advancer, unselected by default)
		// deliberately has no HLTV match, which must NOT block Apply since it's not selected.
		_model.Stage = Stages.Stage2;

		var roster = SixteenTeams();
		_stageRosterService.GetStageRosterAsync(EventId, Stages.Stage2).Returns(roster);
		_stageRosterService.GetLikelyInviteTeamsAsync(EventId, Stages.Stage2).Returns(roster.Where(t => t.PickId > 8).ToList());

		List<HltvRankedTeam> hltvTeams = [.. Enumerable.Range(9, 8).Select(i => new HltvRankedTeam(i, TeamName(i)))];
		_hltvRankingParser.Parse(Arg.Any<Stream>()).Returns(hltvTeams);

		// Act
		await _model.OnPostUploadAsync(FakeFile());

		// Assert
		Assert.True(_model.CanApply);
		Assert.Equal(8, _model.PreviewRows.Count(r => r.Selected));
		Assert.DoesNotContain(_model.PreviewRows, r => r.Selected && !r.Matched);
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
			.. Enumerable.Range(1, 14).Select(i => new PickemsPlanter.Models.Admin.SeedPreviewRow
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
			.. Enumerable.Range(1, 8).Select(i => new PickemsPlanter.Models.Admin.SeedPreviewRow
			{
				TeamName = $"Invite {i}",
				Rank = i,
				Matched = true,
				Selected = true
			}),
			.. Enumerable.Range(1, 8).Select(i => new PickemsPlanter.Models.Admin.SeedPreviewRow
			{
				TeamName = $"Advancer {i}",
				Rank = null,
				Matched = false,
				Selected = false
			})
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
	public async Task OnPostApplyAsync_Stage2_DoesNotApply_WhenMoreThanEightSelected()
	{
		// Arrange
		_model.Stage = Stages.Stage2;
		_model.PreviewRows = [.. Enumerable.Range(1, 9).Select(i => new PickemsPlanter.Models.Admin.SeedPreviewRow
		{
			TeamName = $"Team {i}",
			Rank = i,
			Matched = true,
			Selected = true
		})];

		// Act
		var result = await _model.OnPostApplyAsync();

		// Assert
		Assert.IsType<PageResult>(result);
		Assert.False(_model.CanApply);
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
			.. Enumerable.Range(1, 15).Select(i => new PickemsPlanter.Models.Admin.SeedPreviewRow
			{
				TeamName = $"Team {i}",
				Rank = i,
				Matched = true,
				Selected = true
			})
		];

		// Act
		var result = await _model.OnPostApplyAsync();

		// Assert
		Assert.IsType<PageResult>(result);
		Assert.Contains("Team A", _model.StatusMessage);
		await _seedsTableService.DidNotReceiveWithAnyArgs().UpsertSeedsAsync(default, default!, default!);
	}

	[Fact]
	public async Task OnGetAsync_ShowsAppliedMessage_WhenAppliedFlagSet()
	{
		// Arrange
		_model.Applied = true;

		// Act
		await _model.OnGetAsync();

		// Assert
		Assert.Equal("Seeds applied.", _model.StatusMessage);
	}

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
}
