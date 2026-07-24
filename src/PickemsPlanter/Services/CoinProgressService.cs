using PickemsPlanter.Models.CoinProgress;
using PickemsPlanter.Models.Event;

namespace PickemsPlanter.Services;

public interface ICoinProgressService
{
	Task<CoinProgressResult> GetCoinProgressAsync(string steamId, string eventId);
}

// Rules per blast.tv/major/pickems, assumed the same for every event: 11 total challenges,
// 0-3 completed = Bronze, 4-7 = Silver, 8-10 = Gold, 11 = Diamond. Reuses IPickemsService's
// existing picks/results data (the same source already powering the "Show results" checkmark
// UI) rather than reading the coin item itself, which isn't reliably exposed via the Web API.
public class CoinProgressService(IPickemsService pickemsService) : ICoinProgressService
{
	private const int TotalChallenges = 11;

	public async Task<CoinProgressResult> GetCoinProgressAsync(string steamId, string eventId)
	{
		List<ChallengeProgress> challenges =
		[
			// Granted by buying the event's Viewer Pass — not independently verifiable from
			// app data, but submitting real Steam-backed predictions already requires pass
			// ownership, so any user with predictions on record has necessarily activated it.
			new() { Name = "Coin Activation", Completed = true },
		];

		challenges.AddRange(await GetStageChallengesAsync(Stages.Stage1, "Stage 1", steamId, eventId));
		challenges.AddRange(await GetStageChallengesAsync(Stages.Stage2, "Stage 2", steamId, eventId));
		challenges.AddRange(await GetStageChallengesAsync(Stages.Stage3, "Stage 3", steamId, eventId));
		challenges.AddRange(await GetPlayoffChallengesAsync(steamId, eventId));

		int completed = challenges.Count(c => c.Completed);

		return new CoinProgressResult
		{
			Tier = ResolveTier(completed),
			CompletedChallenges = completed,
			TotalChallenges = TotalChallenges,
			Challenges = challenges
		};
	}

	private async Task<List<ChallengeProgress>> GetStageChallengesAsync(Stages stage, string label, string steamId, string eventId)
	{
		List<string> picks = await pickemsService.GetStagePicksAsync(stage, steamId, eventId);
		List<string> results = await pickemsService.GetStageResultsAsync(stage, eventId);
		bool picksAllowed = await pickemsService.GetStagePicksAllowedAsync(stage, eventId);

		bool participation = HasParticipated(picks, picksAllowed, expectedCount: 10);
		int correct = CountCorrect(picks, results);

		return
		[
			new() { Name = $"{label} Participation", Completed = participation },
			new() { Name = $"{label} Accuracy", Completed = correct >= 5 }
		];
	}

	private async Task<List<ChallengeProgress>> GetPlayoffChallengesAsync(string steamId, string eventId)
	{
		List<string> picks = await pickemsService.GetPlayoffPicksAsync(steamId, eventId);
		List<string> results = await pickemsService.GetPlayoffResultsAsync(eventId);
		bool picksAllowed = await pickemsService.GetPlayoffsPicksAllowedAsync(eventId);

		bool participation = HasParticipated(picks, picksAllowed, expectedCount: 7);

		// Bracket order per the app's own playoffs slicing convention (see toggleCheckmark in
		// stylingFunctions.js): picks 0-3 are the quarter-finals predictions, 4-5 semi-finals, 6 the champion.
		int quarterFinalsCorrect = CountCorrect([.. picks.Take(4)], [.. results.Take(4)]);
		int semiFinalsCorrect = CountCorrect([.. picks.Skip(4).Take(2)], [.. results.Skip(4).Take(2)]);
		bool grandFinalCorrect = IsCorrect(picks.ElementAtOrDefault(6), results.ElementAtOrDefault(6));

		return
		[
			new() { Name = "Playoffs Participation", Completed = participation },
			new() { Name = "Quarter-Finals", Completed = quarterFinalsCorrect >= 2 },
			new() { Name = "Semi-Finals", Completed = semiFinalsCorrect >= 1 },
			new() { Name = "Grand Final", Completed = grandFinalCorrect }
		];
	}

	// Only counts once picks are locked — a still-open stage/playoffs round can't be scored
	// either way yet, since the user could still fill in (or change) their remaining picks.
	private static bool HasParticipated(List<string> picks, bool picksAllowed, int expectedCount) =>
		!picksAllowed && picks.Count == expectedCount && picks.All(pick => !pick.Contains("unknown"));

	private static int CountCorrect(IReadOnlyList<string> picks, IReadOnlyList<string> results)
	{
		int count = 0;

		for (int i = 0; i < picks.Count && i < results.Count; i++)
		{
			if (IsCorrect(picks[i], results[i]))
				count++;
		}

		return count;
	}

	private static bool IsCorrect(string? pick, string? result) =>
		pick is not null && result is not null && pick == result && !pick.Contains("unknown");

	private static CoinTier ResolveTier(int completed) => completed switch
	{
		TotalChallenges => CoinTier.Diamond,
		>= 8 => CoinTier.Gold,
		>= 4 => CoinTier.Silver,
		_ => CoinTier.Bronze
	};
}
