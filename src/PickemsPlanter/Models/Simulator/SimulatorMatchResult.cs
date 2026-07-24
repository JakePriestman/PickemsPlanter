namespace PickemsPlanter.Models.Simulator;

public class SimulatorMatchResult
{
	public required string WinnerTeam { get; init; }

	public required string LoserTeam { get; init; }

	public required int Round { get; init; }

	// "{winnerScore}-{loserScore}" series score (eg. "2-1" for a BO3), from PandaScore's
	// per-team Results. Null when Results didn't have a score for both sides.
	public string? Score { get; init; }

	public required bool IsBestOfThree { get; init; }
}
