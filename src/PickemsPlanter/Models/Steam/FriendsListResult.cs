using System.Text.Json.Serialization;

namespace PickemsPlanter.Models.Steam;

public class FriendsListResult
{
	[JsonPropertyName("friendslist")]
	public required FriendsListContainer FriendsList { get; init; }
}

public class FriendsListContainer
{
	public IReadOnlyCollection<SteamFriend> Friends { get; init; } = [];
}

public class SteamFriend
{
	[JsonPropertyName("steamid")]
	public required string SteamId { get; init; }
}
