using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PickemsPlanter.APIs;
using PickemsPlanter.Models.Configurations;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.PandaScore;

namespace PickemsPlanter.Services;

public interface IPandaScoreResultsCachingService
{
	IReadOnlyCollection<PandaScoreMatch> GetCompletedMatches(string eventId, Stages stage);
	IReadOnlyCollection<PandaScoreMatch> GetLiveMatches(string eventId, Stages stage);
}

public class PandaScoreResultsCachingService(
	IPandaScoreApi pandaScoreApi,
	IEventTableService eventTableService,
	IMemoryCache cache,
	IOptionsMonitor<PandaScoreConfig> config) : IHostedService, IPandaScoreResultsCachingService
{
	private readonly PandaScoreConfig _config = config.CurrentValue;
	private CancellationTokenSource? _cts;
	private Task? _pollingTask;

	public Task StartAsync(CancellationToken cancellationToken)
	{
		if (!_config.Enabled)
			return Task.CompletedTask;

		_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_pollingTask = PollLoopAsync(_cts.Token);

		return Task.CompletedTask;
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		_cts?.Cancel();

		if (_pollingTask is not null)
		{
			try
			{
				await _pollingTask;
			}
			catch (OperationCanceledException)
			{
				// expected on shutdown
			}
		}
	}

	public IReadOnlyCollection<PandaScoreMatch> GetCompletedMatches(string eventId, Stages stage)
	{
		if (cache.TryGetValue(CacheKey(eventId, stage), out IReadOnlyCollection<PandaScoreMatch>? matches) && matches is not null)
			return matches;

		return [];
	}

	public IReadOnlyCollection<PandaScoreMatch> GetLiveMatches(string eventId, Stages stage)
	{
		if (cache.TryGetValue(LiveCacheKey(eventId, stage), out IReadOnlyCollection<PandaScoreMatch>? matches) && matches is not null)
			return matches;

		return [];
	}

	// Public so it can be exercised directly in tests without driving the polling loop/timer.
	public async Task RefreshAllAsync()
	{
		var events = await eventTableService.GetAllEventsAsync();

		foreach (var @event in events.Where(e => !e.Disabled))
		{
			await RefreshStageAsync(@event.Id, Stages.Stage1, @event.PandaScoreStage1TournamentId);
			await RefreshStageAsync(@event.Id, Stages.Stage2, @event.PandaScoreStage2TournamentId);
			await RefreshStageAsync(@event.Id, Stages.Stage3, @event.PandaScoreStage3TournamentId);
		}
	}

	private async Task PollLoopAsync(CancellationToken cancellationToken)
	{
		int intervalMinutes = Math.Max(1, _config.PollingIntervalMinutes);

		using PeriodicTimer timer = new(TimeSpan.FromMinutes(intervalMinutes));

		while (true)
		{
			await RefreshAllAsync();

			if (!await timer.WaitForNextTickAsync(cancellationToken))
				return;
		}
	}

	private async Task RefreshStageAsync(string eventId, Stages stage, int? tournamentId)
	{
		if (tournamentId is null)
			return;

		var matches = await pandaScoreApi.GetTournamentMatchesAsync(tournamentId.Value);

		// null means the call failed/shape was unexpected — leave whatever is already cached in place
		// rather than wiping out a previously good result with an empty one.
		if (matches is null)
			return;

		var completed = matches
			.Where(m => m.Status == "finished" && m.WinnerId is not null)
			.ToList();

		var live = matches
			.Where(m => m.Status == "running")
			.ToList();

		cache.Set(CacheKey(eventId, stage), (IReadOnlyCollection<PandaScoreMatch>)completed);
		cache.Set(LiveCacheKey(eventId, stage), (IReadOnlyCollection<PandaScoreMatch>)live);
	}

	private static string CacheKey(string eventId, Stages stage) => $"PANDASCORE_{eventId}_{stage}";
	private static string LiveCacheKey(string eventId, Stages stage) => $"PANDASCORE_LIVE_{eventId}_{stage}";
}
