using Microsoft.Extensions.Caching.Memory;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.StorageAccount;

namespace PickemsPlanter.Services
{

	public interface ISeedsCachingService
	{
		Task<IReadOnlyCollection<Seed>> GetSeedsInStageAsync(Stages stage, string eventId);
	}

	public class SeedsCachingService(ISeedsTableService tableService, IMemoryCache cache) : ISeedsCachingService
	{
		public async Task<IReadOnlyCollection<Seed>> GetSeedsInStageAsync(Stages stage, string eventId)
		{
			string key = $"TOURNAMENT_{eventId}_{stage}";

			if (!cache.TryGetValue(key, out IReadOnlyCollection<Seed>? seeds))
			{
				IReadOnlyCollection<Seed> seedsFromTable = await tableService.GetSeedsInStageAsync(stage, eventId);
				seeds = seedsFromTable;
				cache.Set(key, seedsFromTable, TimeSpan.FromMinutes(10));
			}

			return seeds!;
		}
	}
}
