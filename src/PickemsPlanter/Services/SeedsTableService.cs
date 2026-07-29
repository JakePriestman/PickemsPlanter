using Azure;
using Azure.Data.Tables;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.StorageAccount;

namespace PickemsPlanter.Services;

public interface ISeedsTableService
{
	Task<IReadOnlyCollection<Seed>> GetSeedsInStageAsync(Stages stage, string eventId);
	Task UpsertSeedsAsync(Stages stage, string eventId, IReadOnlyDictionary<string, int> teamNameToRank);
}

public class SeedsTableService(TableServiceClient tableServiceClient) : ISeedsTableService
{
	public async Task<IReadOnlyCollection<Seed>> GetSeedsInStageAsync(Stages stage, string eventId)
	{
		string tableName = stage.ToString().ToLower();
		TableClient client = tableServiceClient.GetTableClient(tableName);

		AsyncPageable<Seed> query = client.QueryAsync<Seed>(x => x.PartitionKey == eventId);

		List<Seed> results = [];

		await foreach (var item in query)
		{
			results.Add(item);
		}

		return [.. results.OrderBy(s => s.Rank)];
	}

	public async Task UpsertSeedsAsync(Stages stage, string eventId, IReadOnlyDictionary<string, int> teamNameToRank)
	{
		string tableName = stage.ToString().ToLower();
		TableClient client = tableServiceClient.GetTableClient(tableName);

		foreach (var (teamName, rank) in teamNameToRank)
		{
			TableEntity entity = new(eventId, teamName)
			{
				{ nameof(Seed.Rank), rank }
			};

			await client.UpsertEntityAsync(entity, TableUpdateMode.Merge);
		}
	}
}
