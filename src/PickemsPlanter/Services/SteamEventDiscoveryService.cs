using Microsoft.Extensions.Options;
using PickemsPlanter.APIs;
using PickemsPlanter.Models.Configurations;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.PandaScore;
using System.Text.RegularExpressions;

namespace PickemsPlanter.Services;

public partial class SteamEventDiscoveryService(
	ISteamAPI steamAPI,
	IEventTableService eventTableService,
	IPandaScoreApi pandaScoreApi,
	IOptionsMonitor<EventDiscoveryConfig> config) : IHostedService
{
	private readonly EventDiscoveryConfig _config = config.CurrentValue;
	private CancellationTokenSource? _cts;
	private Task? _pollingTask;

	private static readonly string[] StopWords =
	[
		"cs2", "cs", "csgo", "counter-strike", "counter-strike:", "global", "offensive",
		"major", "championship", "the", "and", "&"
	];

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

	private async Task PollLoopAsync(CancellationToken cancellationToken)
	{
		int intervalMinutes = Math.Max(1, _config.PollingIntervalMinutes);

		using PeriodicTimer timer = new(TimeSpan.FromMinutes(intervalMinutes));

		while (true)
		{
			await RunDiscoveryAsync();

			if (!await timer.WaitForNextTickAsync(cancellationToken))
				return;
		}
	}

	// Public so it can be exercised directly in tests without driving the polling loop/timer.
	public async Task RunDiscoveryAsync()
	{
		var events = await eventTableService.GetAllEventsAsync();

		await RetireStaleEventsAsync(events);
		await DiscoverNewEventsAsync(events);
	}

	private async Task RetireStaleEventsAsync(IReadOnlyCollection<Event> events)
	{
		foreach (var @event in events.Where(e => !e.Disabled))
		{
			try
			{
				await steamAPI.GetTournamentLayoutAsync(@event.Id);
			}
			catch (KeyNotFoundException)
			{
				await eventTableService.UpsertEventAsync(@event.Id, @event.Name, disabled: true);
			}
		}
	}

	private async Task DiscoverNewEventsAsync(IReadOnlyCollection<Event> events)
	{
		int highestKnownId = 0;

		foreach (var @event in events)
		{
			if (int.TryParse(@event.Id, out int id) && id > highestKnownId)
				highestKnownId = id;
		}

		int lookahead = Math.Max(1, _config.LookaheadCount);

		for (int candidateId = highestKnownId + 1; candidateId <= highestKnownId + lookahead; candidateId++)
		{
			string candidateEventId = candidateId.ToString();

			string? name;

			try
			{
				var result = await steamAPI.GetTournamentLayoutAsync(candidateEventId);
				name = result.Result.Name;
			}
			catch (KeyNotFoundException)
			{
				// Nothing at this id yet — keep checking the rest of the lookahead window in case of a gap.
				continue;
			}

			await eventTableService.UpsertEventAsync(candidateEventId, name, disabled: false);

			await TryAutoResolvePandaScoreMappingAsync(candidateEventId, name);
		}
	}

	private async Task TryAutoResolvePandaScoreMappingAsync(string eventId, string eventName)
	{
		int? year = ExtractYear(eventName);

		if (year is null)
			return;

		Dictionary<int, PandaScoreSeries> candidateSeries = [];

		foreach (var keyword in ExtractSearchKeywords(eventName))
		{
			var series = await pandaScoreApi.SearchSeriesAsync(keyword);

			if (series is null)
				continue;

			foreach (var s in series.Where(s => s.Year == year))
				candidateSeries[s.Id] = s;
		}

		// Zero or ambiguous matches — leave it for manual mapping rather than guess.
		if (candidateSeries.Count != 1)
			return;

		var matchedSeries = candidateSeries.Values.Single();

		int? stage1 = FindTournamentId(matchedSeries, "stage 1");
		int? stage2 = FindTournamentId(matchedSeries, "stage 2");
		int? stage3 = FindTournamentId(matchedSeries, "stage 3");

		if (stage1 is null || stage2 is null || stage3 is null)
			return;

		await eventTableService.SetPandaScoreTournamentIdsAsync(eventId, stage1, stage2, stage3);
	}

	private static int? FindTournamentId(PandaScoreSeries series, string stageNameContains)
	{
		var matches = series.Tournaments
			.Where(t => t.Name.Contains(stageNameContains, StringComparison.OrdinalIgnoreCase))
			.ToList();

		return matches.Count == 1 ? matches[0].Id : null;
	}

	private static int? ExtractYear(string eventName)
	{
		var match = YearRegex().Match(eventName);

		return match.Success ? int.Parse(match.Value) : null;
	}

	private static IEnumerable<string> ExtractSearchKeywords(string eventName)
	{
		return eventName
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Select(w => w.Trim())
			.Where(w => w.Length > 2)
			.Where(w => !StopWords.Contains(w, StringComparer.OrdinalIgnoreCase))
			.Where(w => !YearRegex().IsMatch(w))
			.Distinct(StringComparer.OrdinalIgnoreCase);
	}

	[GeneratedRegex(@"\b(20\d{2})\b")]
	private static partial Regex YearRegex();
}
