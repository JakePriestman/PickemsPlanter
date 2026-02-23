using Azure;
using Azure.Data.Tables;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.StorageAccount;
namespace PickemsPlanter.Services
{
	public interface ISeedsTableService
	{
		Task<IReadOnlyCollection<Seed>> GetSeedsInStageAsync(Stages stage, string eventId);
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
	}
}
