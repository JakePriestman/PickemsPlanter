namespace PickemsPlanter.Models.Configurations;

public class EventDiscoveryConfig
{
	public bool Enabled { get; init; } = true;

	public int PollingIntervalMinutes { get; init; } = 60;

	public int LookaheadCount { get; init; } = 5;
}
