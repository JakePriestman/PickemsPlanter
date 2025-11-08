namespace PickemsPlanter.Models.Steam
{
	public class Section
	{
		public int SectionId { get; init; }
		public string Name { get; init; }
		public IEnumerable<Group> Groups { get; init; }
	}
}
