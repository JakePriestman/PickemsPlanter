namespace PickemsPlanter.Models.Configurations;

public class PandaScoreConfig
{
	public required string ApiToken { get; init; }

	public bool Enabled { get; init; } = true;

	public int PollingIntervalMinutes { get; init; } = 10;
}
