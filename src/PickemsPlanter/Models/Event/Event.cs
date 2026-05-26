namespace PickemsPlanter.Models.Event;

public class Event
{

	public required string Id { get; init; }

	public required string Name { get; init; }

	public required bool Disabled { get; init; }
}
