namespace PickemsPlanter.Models.Simulator;

public class SimulatorMatchResult
{
	public required string WinnerTeam { get; init; }

	public required string LoserTeam { get; init; }

	public required int Round { get; init; }
}
