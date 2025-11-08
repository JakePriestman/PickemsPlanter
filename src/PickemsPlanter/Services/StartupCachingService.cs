using PickemsPlanter.APIs;
using PickemsPlanter.Models;
using PickemsPlanter.Models.Steam;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace PickemsPlanter.Services
{
	public class StartupCachingService(IMemoryCache cache, ISteamAPI steamAPI, IWebHostEnvironment env, JsonSerializerOptions options) : IHostedService
	{
		private readonly IMemoryCache _cache = cache;
		private readonly ISteamAPI _steamAPI = steamAPI;
		private readonly IWebHostEnvironment _env = env;
		private readonly JsonSerializerOptions _options = options;

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var filePath = Path.Combine(_env.ContentRootPath, "events.json");

			using var stream = File.OpenRead(filePath);

			var events = JsonSerializer.Deserialize<IReadOnlyCollection<Event>>(stream, _options);

			foreach (var @event in events!)
			{
				var tournamentLayout = await _steamAPI.GetTournamentLayoutAsync(@event.Id);

				if (tournamentLayout.Result is not null)
				{
					_cache.Set($"TOURNAMENT_{@event.Id}_TEAMS", tournamentLayout.Result.Teams);

					_cache.Set($"TOURNAMENT_{@event.Id}_{Stages.Stage1}", tournamentLayout.Result.Sections[(int)Stages.Stage1]);
					_cache.Set($"TOURNAMENT_{@event.Id}_{Stages.Stage2}", tournamentLayout.Result.Sections[(int)Stages.Stage2]);
					_cache.Set($"TOURNAMENT_{@event.Id}_{Stages.Stage3}", tournamentLayout.Result.Sections[(int)Stages.Stage3]);

					tournamentLayout.Result.Sections.Remove(tournamentLayout.Result.Sections[0]);
					tournamentLayout.Result.Sections.Remove(tournamentLayout.Result.Sections[0]);
					tournamentLayout.Result.Sections.Remove(tournamentLayout.Result.Sections[0]);

					_cache.Set($"TOURNAMENT_{@event.Id}_{Stages.Playoffs}", tournamentLayout.Result.Sections);
				}
			}
		}

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
