using Azure.Data.Tables;
using CS2Pickems.Models.TableStorage;

namespace CS2Pickems.Services
{
	public interface ITableStorageService
	{
		Task<bool> ExistsAsync(string steamId, string eventId);
		Task CreateEntryAsync(UserEvent userEvent);
		Task<UserEvent?> GetEntryIfExistsAsync(string steamId, string eventId);
	}

	public class TableStorageService(TableServiceClient tableServiceClient) : ITableStorageService
	{
		private readonly TableClient _client = tableServiceClient.GetTableClient("userEvents");

		public async Task<bool> ExistsAsync(string steamId, string eventId)
		{
			var response = await _client.GetEntityIfExistsAsync<UserEvent>(steamId, eventId);

			return response.HasValue;
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
	}
}
