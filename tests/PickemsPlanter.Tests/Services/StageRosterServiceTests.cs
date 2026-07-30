using NSubstitute;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.Steam;
using Xunit;

namespace PickemsPlanter.Services;

public class StageRosterServiceTests
{
	private readonly StageRosterService _service;
	private readonly ITournamentCachingService _tournamentCachingService = Substitute.For<ITournamentCachingService>();

	public StageRosterServiceTests()
	{
		_service = new(_tournamentCachingService);
	}

	private static Team Team(int pickId) => new() { PickId = pickId, Name = $"Team {pickId}", Logo = $"team{pickId}" };

	private static Section SectionWithPickIds(IEnumerable<int> pickIds) => new()
	{
		SectionId = 1,
		Name = "Section",
		Groups = [new Group { GroupId = 1, Name = "Group", Teams = [.. pickIds.Select(id => new TeamId { PickId = id })] }]
	};

	[Fact]
	public async Task GetStageRosterAsync_ReturnsOnlyTeamsInTheSectionsPickIds()
	{
		// Arrange
		string eventId = "25";
		List<Team> allTeams = [.. Enumerable.Range(1, 20).Select(Team)];

		_tournamentCachingService.GetSectionAsync(eventId, Stages.Stage1).Returns(SectionWithPickIds(Enumerable.Range(1, 16)));
		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(allTeams);

		// Act
		var roster = await _service.GetStageRosterAsync(eventId, Stages.Stage1);

		// Assert
		Assert.Equal(16, roster.Count);
		Assert.Equal(Enumerable.Range(1, 16).ToHashSet(), roster.Select(t => t.PickId).ToHashSet());
	}

	[Fact]
	public async Task GetStageRosterAsync_PreservesTheSectionsTeamOrder_NotGetTournamentTeamsAsyncsOrder()
	{
		// Arrange — the team list comes back in a different order than the section lists them.
		string eventId = "25";
		List<Team> allTeams = [.. new[] { 3, 1, 2 }.Select(Team)];

		_tournamentCachingService.GetSectionAsync(eventId, Stages.Stage1).Returns(SectionWithPickIds([1, 2, 3]));
		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(allTeams);

		// Act
		var roster = await _service.GetStageRosterAsync(eventId, Stages.Stage1);

		// Assert
		Assert.Equal([1, 2, 3], roster.Select(t => t.PickId));
	}

	[Theory]
	[InlineData(Stages.Stage1)]
	[InlineData(Stages.Playoffs)]
	public async Task GetLikelyInviteTeamsAsync_ReturnsNull_WhenStageHasNoPreviousStage(Stages stage)
	{
		// Act
		var invites = await _service.GetLikelyInviteTeamsAsync("25", stage);

		// Assert
		Assert.Null(invites);
		await _tournamentCachingService.DidNotReceiveWithAnyArgs().GetSectionAsync(default!, default);
	}

	[Fact]
	public async Task GetLikelyInviteTeamsAsync_ReturnsNull_WhenCurrentRosterIsNot16Teams()
	{
		// Arrange
		string eventId = "25";
		List<Team> allTeams = [.. Enumerable.Range(1, 20).Select(Team)];

		_tournamentCachingService.GetSectionAsync(eventId, Stages.Stage2).Returns(SectionWithPickIds(Enumerable.Range(1, 15)));
		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(allTeams);

		// Act
		var invites = await _service.GetLikelyInviteTeamsAsync(eventId, Stages.Stage2);

		// Assert
		Assert.Null(invites);
	}

	[Fact]
	public async Task GetLikelyInviteTeamsAsync_ReturnsNull_WhenPreviousStageRosterIsNot16Teams()
	{
		// Arrange
		string eventId = "25";
		List<Team> allTeams = [.. Enumerable.Range(1, 20).Select(Team)];

		_tournamentCachingService.GetSectionAsync(eventId, Stages.Stage2).Returns(SectionWithPickIds(Enumerable.Range(1, 16)));
		_tournamentCachingService.GetSectionAsync(eventId, Stages.Stage1).Returns(SectionWithPickIds(Enumerable.Range(1, 10)));
		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(allTeams);

		// Act
		var invites = await _service.GetLikelyInviteTeamsAsync(eventId, Stages.Stage2);

		// Assert
		Assert.Null(invites);
	}

	[Fact]
	public async Task GetLikelyInviteTeamsAsync_ReturnsTeamsNewToThisStage_WhenExactlyEight()
	{
		// Arrange — Stage1 roster is pickids 1-16; Stage2 roster is pickids 9-24 (8 advancers
		// shared with Stage1: 9-16, plus 8 new invites: 17-24).
		string eventId = "25";
		List<Team> allTeams = [.. Enumerable.Range(1, 24).Select(Team)];

		_tournamentCachingService.GetSectionAsync(eventId, Stages.Stage1).Returns(SectionWithPickIds(Enumerable.Range(1, 16)));
		_tournamentCachingService.GetSectionAsync(eventId, Stages.Stage2).Returns(SectionWithPickIds(Enumerable.Range(9, 16)));
		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(allTeams);

		// Act
		var invites = await _service.GetLikelyInviteTeamsAsync(eventId, Stages.Stage2);

		// Assert
		Assert.NotNull(invites);
		Assert.Equal(Enumerable.Range(17, 8).ToHashSet(), invites.Select(t => t.PickId).ToHashSet());
	}

	[Fact]
	public async Task GetLikelyInviteTeamsAsync_ReturnsFirstEightInSteamsOwnOrder_WhenRosterIsInflatedBeyondSixteen()
	{
		// Arrange — Steam reports 24 for a stage whose previous stage hasn't concluded yet:
		// this stage's 8 confirmed invites (always listed first, per PickemsService's existing
		// live-picker logic) plus the previous stage's full 16-team candidate pool. Section
		// order deliberately differs from pickid numeric order, to prove it's Steam's own team
		// order being sliced, not GetTournamentTeamsAsync's.
		string eventId = "25";
		List<Team> allTeams = [.. Enumerable.Range(1, 16).Select(Team), .. Enumerable.Range(101, 8).Select(Team)];

		int[] sectionOrder = [.. Enumerable.Range(101, 8), .. Enumerable.Range(1, 16)];

		_tournamentCachingService.GetSectionAsync(eventId, Stages.Stage2).Returns(SectionWithPickIds(sectionOrder));
		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(allTeams);

		// Act
		var invites = await _service.GetLikelyInviteTeamsAsync(eventId, Stages.Stage2);

		// Assert
		Assert.NotNull(invites);
		Assert.Equal(Enumerable.Range(101, 8), invites.Select(t => t.PickId));
	}

	[Fact]
	public async Task GetLikelyInviteTeamsAsync_ReturnsNull_WhenDiffIsNotExactlyEight()
	{
		// Arrange — only 4 teams are new to Stage3 vs Stage2, so the diff can't be trusted as
		// "the 8 invites".
		string eventId = "25";
		List<Team> allTeams = [.. Enumerable.Range(1, 20).Select(Team)];

		_tournamentCachingService.GetSectionAsync(eventId, Stages.Stage2).Returns(SectionWithPickIds(Enumerable.Range(1, 16)));
		_tournamentCachingService.GetSectionAsync(eventId, Stages.Stage3).Returns(SectionWithPickIds(Enumerable.Range(13, 16)));
		_tournamentCachingService.GetTournamentTeamsAsync(eventId).Returns(allTeams);

		// Act
		var invites = await _service.GetLikelyInviteTeamsAsync(eventId, Stages.Stage3);

		// Assert
		Assert.Null(invites);
	}
}
