using Azure.Data.Tables;
using CS2Pickems.Models.TableStorage;

namespace CS2Pickems.Services
{
	public interface ITableStorageService
	{
		Task<bool> ExistsAsync(string steamId, string eventId);
		Task CreateUserEventIfNotExistsAsync(string steamId, string eventId, string authCode);
		Task CreateUserEventAsync(string steamId, string eventId, string authCode);
		Task CreateEntryAsync(UserEvent userEvent);
		Task<UserEvent?> GetEntryIfExistsAsync(string steamId, string eventId);
		Task DeleteEntityIfExistsAsync(string steamId, string eventId);
	}

	public class TableStorageService(TableServiceClient tableServiceClient) : ITableStorageService
	{
		private readonly TableClient _client = tableServiceClient.GetTableClient("userEvents");

		public async Task<bool> ExistsAsync(string steamId, string eventId)
		{
			var response = await _client.GetEntityIfExistsAsync<UserEvent>(steamId, eventId);

			return response.HasValue;
		}
		public async Task CreateUserEventIfNotExistsAsync(string steamId, string eventId, string authCode)
		{
			bool exists = await ExistsAsync(steamId, eventId);

			if (!exists)
				await CreateUserEventAsync(steamId, eventId, authCode);
		}
		public async Task CreateUserEventAsync(string steamId, string eventId, string authCode)
		{
			UserEvent newUserEvent = new()
			{
				PartitionKey = steamId,
				RowKey = eventId,
				AuthCode = authCode
			};

			await CreateEntryAsync(newUserEvent);
		}

		public async Task CreateEntryAsync(UserEvent userEvent)
		{
			await _client.AddEntityAsync(userEvent);
		}

		public async Task<UserEvent?> GetEntryIfExistsAsync(string steamId, string eventId)
		{
			var response = await _client.GetEntityIfExistsAsync<UserEvent>(steamId, eventId);

			if (response.HasValue)
				return response.Value;

			return null;
		}

		public async Task DeleteEntityIfExistsAsync(string steamId, string eventId)
		{

			var exists = await ExistsAsync(steamId, eventId);

			if (exists)
			{
				var response = await _client.DeleteEntityAsync(steamId, eventId);
			}
		}
	}
}
