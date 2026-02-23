using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.Simulator;
using PickemsPlanter.Models.Steam;
using PickemsPlanter.Models.StorageAccount;

namespace PickemsPlanter.Services
{
	public interface ISeedsService
	{
		Task<List<Seeds>> GetSeedImagesInStageAsync(Stages stage, string eventId);
	}

	public class SeedsService(ISeedsTableService seedsTableService, ITournamentCachingService tournamentCachingService) : ISeedsService
	{
		public async Task<List<Seeds>> GetSeedImagesInStageAsync(Stages stage, string eventId)
		{
			IReadOnlyCollection<Seed> seeds = await seedsTableService.GetSeedsInStageAsync(stage, eventId);

			IReadOnlyCollection<Team> teams = await tournamentCachingService.GetTournamentTeamsAsync(eventId);

			var seedsForSimulator = new List<Seeds>();

			foreach (var seed in seeds)
			{
				var logo = teams.FirstOrDefault(x => x.Name.Equals(seed.RowKey, StringComparison.CurrentCultureIgnoreCase))?.Logo;

				var imageUrl = string.Empty;

				if (logo is null)
					imageUrl = "unknown.png";
				else
					imageUrl = $"{logo}.png";

				seedsForSimulator.Add(new Seeds
				{
					Seed = seed.Rank.ToString(),
					Team = imageUrl
				});
			}

			while (seedsForSimulator.Count < 16)
			{
				seedsForSimulator.Add(new Seeds
				{
					Seed = (seedsForSimulator.Count + 1).ToString(),
					Team = "unknown.png"
				});
			}

			return seedsForSimulator;
		}
	}
}
