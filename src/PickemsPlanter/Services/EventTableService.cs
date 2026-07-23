using Azure;
using Azure.Data.Tables;
using PickemsPlanter.Models.Event;

namespace PickemsPlanter.Services;

public interface IEventTableService
{
	Task<IReadOnlyCollection<Event>> GetAllEventsAsync();
	Task UpsertEventAsync(string eventId, string eventName, bool disabled);
	Task SetPandaScoreTournamentIdsAsync(string eventId, int? stage1TournamentId, int? stage2TournamentId, int? stage3TournamentId);
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
				Name = item.Name,
				Disabled = item.Disabled,
				PandaScoreStage1TournamentId = item.PandaScoreStage1TournamentId,
				PandaScoreStage2TournamentId = item.PandaScoreStage2TournamentId,
				PandaScoreStage3TournamentId = item.PandaScoreStage3TournamentId
			};

			results.Add(eventModel);
		}

		return results;
	}

	public async Task UpsertEventAsync(string eventId, string eventName, bool disabled)
	{
		Models.StorageAccount.Event newEvent = new()
		{
			PartitionKey = "Event",
			RowKey = eventId,
			Name = eventName,
			Disabled = disabled
		};

		await _client.UpsertEntityAsync(newEvent);
	}

	public async Task SetPandaScoreTournamentIdsAsync(string eventId, int? stage1TournamentId, int? stage2TournamentId, int? stage3TournamentId)
	{
		// Merge (not the strongly-typed Event) so Name/Disabled are left untouched.
		TableEntity entity = new("Event", eventId)
		{
			{ nameof(Models.StorageAccount.Event.PandaScoreStage1TournamentId), stage1TournamentId },
			{ nameof(Models.StorageAccount.Event.PandaScoreStage2TournamentId), stage2TournamentId },
			{ nameof(Models.StorageAccount.Event.PandaScoreStage3TournamentId), stage3TournamentId }
		};

		await _client.UpsertEntityAsync(entity, TableUpdateMode.Merge);
	}
}
