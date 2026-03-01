namespace PickemsPlanter.Models.Steam;

public class Team
{
	public string? Type { get; set; }
	public ulong ItemId { get; set; }
	public int TeamId { get; set; }
	public int PickId { get; set; }
	public required string Logo { get; set; }
	public required string Name { get; set; }
}
