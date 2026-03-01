using PickemsPlanter.APIs;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using PickemsPlanter.Models.Event;

namespace PickemsPlanter.Services;

public class StartupCachingService(IMemoryCache cache, ISteamAPI steamAPI, IEventTableService eventTableService) : IHostedService
{
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		var events = await eventTableService.GetAllEventsAsync();

		foreach (var @event in events!)
		{
			var tournamentLayout = await steamAPI.GetTournamentLayoutAsync(@event.Id);

			if (tournamentLayout.Result is not null)
			{
				cache.Set($"TOURNAMENT_{@event.Id}_TEAMS", tournamentLayout.Result.Teams);

				cache.Set($"TOURNAMENT_{@event.Id}_{Stages.Stage1}", tournamentLayout.Result.Sections[(int)Stages.Stage1]);
				cache.Set($"TOURNAMENT_{@event.Id}_{Stages.Stage2}", tournamentLayout.Result.Sections[(int)Stages.Stage2]);
				cache.Set($"TOURNAMENT_{@event.Id}_{Stages.Stage3}", tournamentLayout.Result.Sections[(int)Stages.Stage3]);

				tournamentLayout.Result.Sections.Remove(tournamentLayout.Result.Sections[0]);
				tournamentLayout.Result.Sections.Remove(tournamentLayout.Result.Sections[0]);
				tournamentLayout.Result.Sections.Remove(tournamentLayout.Result.Sections[0]);

				cache.Set($"TOURNAMENT_{@event.Id}_{Stages.Playoffs}", tournamentLayout.Result.Sections);
			}
		}
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
