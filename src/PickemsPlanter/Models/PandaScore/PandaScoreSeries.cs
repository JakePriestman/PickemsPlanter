namespace PickemsPlanter.Models.PandaScore;

public class PandaScoreSeries
{
	public int Id { get; init; }

	public required string Name { get; init; }

	public int Year { get; init; }

	public string? Slug { get; init; }

	public IReadOnlyCollection<PandaScoreTournamentSummary> Tournaments { get; init; } = [];
}
