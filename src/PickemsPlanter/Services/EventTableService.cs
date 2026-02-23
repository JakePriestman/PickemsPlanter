using Azure;
using Azure.Data.Tables;
using PickemsPlanter.Models.Event;

namespace PickemsPlanter.Services
{
	public interface IEventTableService
	{
		Task<IReadOnlyCollection<Event>> GetAllEventsAsync();
		Task UpsertEventAsync(string eventId, string eventName);
	}

	public class EventTableService(TableServiceClient tableServiceClient) : IEventTableService
	{
		private readonly TableClient _client = tableServiceClient.GetTableClient("events");

		public async Task<IReadOnlyCollection<Event>> GetAllEventsAsync()
		{
			AsyncPageable<Models.StorageAccount.Event> query = _client.QueryAsync<Models.StorageAccount.Event>(x => x.PartitionKey == "Event");

			List<Event> results = [];

			await foreach (var item in query)
			{
				var eventModel = new Event
				{
					Id = item.RowKey,
					Name = item.Name
				};

				results.Add(eventModel);
			}

			return results;
		}

		public async Task UpsertEventAsync(string eventId, string eventName)
		{
			Models.StorageAccount.Event newEvent = new()
			{
				PartitionKey = "Event",
				RowKey = eventId,
				Name = eventName
			};

			await _client.UpsertEntityAsync(newEvent);
		}
	}
}
