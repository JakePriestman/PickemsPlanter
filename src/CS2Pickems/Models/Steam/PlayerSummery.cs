namespace CS2Pickems.Models.Steam
{
	public class PlayerSummery
	{
		public required string SteamId { get; init; }
		public int CommunityVisibilityState { get; init; }
		public int ProfileState { get; init; }
		public required string PersonaName { get; init; }
		public required string ProfileUrl { get; init; }
		public required string Avatar { get; init; }
		public required string AvatarMedium { get; init; }
		public required string AvatarFull { get; init; }
		public required string AvatarHash { get; init; }
		public long LastLogOff { get; init; }
		public int PersonaState { get; init; }
		public required string PrimaryClanId { get; init; }
		public long TimeCreated { get; init; }
		public int PersonaStatFlags { get; init; }
	}
}
