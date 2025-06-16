using CS2Pickems.APIs;
using Microsoft.Extensions.Caching.Memory;

namespace CS2Pickems.Services
{
	public class CachingService(IMemoryCache cache, ISteamAPI steamAPI) : IHostedService
	{
		private readonly IMemoryCache _cache = cache;
		private readonly ISteamAPI _steamAPI = steamAPI;

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var tournamentLayout = await _steamAPI.GetTournamentLayoutAsync();

			var tournamentItems = await _steamAPI.GetTournamentItemsAsync();

			_cache.Set("STAGE_1", tournamentLayout.Result.Sections[0]);
			_cache.Set("STAGE_2", tournamentLayout.Result.Sections[1]);
			_cache.Set("STAGE_3", tournamentLayout.Result.Sections[2]);

			tournamentLayout.Result.Sections.Remove(tournamentLayout.Result.Sections[0]);
			tournamentLayout.Result.Sections.Remove(tournamentLayout.Result.Sections[0]);
			tournamentLayout.Result.Sections.Remove(tournamentLayout.Result.Sections[0]);

			_cache.Set("PLAYOFFS", tournamentLayout.Result.Sections);


			foreach (var team in tournamentLayout.Result.Teams)
			{
				var teamFromItems = tournamentItems.Result.Items.First(x => x.TeamId == team.PickId);

				team.ItemId = teamFromItems.ItemId;
				team.TeamId = teamFromItems.TeamId;
				team.Type = teamFromItems.Type;
			}

			_cache.Set("TEAMS", tournamentLayout.Result.Teams);
		}

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
