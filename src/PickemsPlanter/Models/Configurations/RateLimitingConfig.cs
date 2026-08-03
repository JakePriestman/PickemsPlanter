namespace PickemsPlanter.Models.Configurations;

public class RateLimitingConfig
{
	public int SteamApiPermitLimit { get; init; } = 60;

	public int SteamApiWindowMinutes { get; init; } = 1;

	public int AuthPermitLimit { get; init; } = 10;

	public int AuthWindowMinutes { get; init; } = 1;
}
