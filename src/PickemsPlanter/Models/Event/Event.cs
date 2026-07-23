namespace PickemsPlanter.Models.Event;

public class Event
{

	public required string Id { get; init; }

	public required string Name { get; init; }

	public required bool Disabled { get; set; }

	public int? PandaScoreStage1TournamentId { get; init; }

	public int? PandaScoreStage2TournamentId { get; init; }

	public int? PandaScoreStage3TournamentId { get; init; }
}
