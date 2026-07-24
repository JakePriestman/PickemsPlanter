using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Services;

namespace PickemsPlanter.Pages;

public class SimulatorModel(ISeedsService seedsService, IPandaScoreResultsService pandaScoreResultsService) : PageModel
{
	[BindProperty(SupportsGet = true)]
	public required string EventId { get; init; }

	[BindProperty(SupportsGet = true)]
	public required Stages Stage { get; init; }

	public async Task<JsonResult> OnGetSeeds()
	{
		return new JsonResult(await seedsService.GetSeedImagesInStageAsync(Stage, EventId));
	}

	public async Task<JsonResult> OnGetResults()
	{
		return new JsonResult(await pandaScoreResultsService.GetCompletedMatchesAsync(EventId, Stage));
	}

	public async Task<JsonResult> OnGetLiveMatches()
	{
		return new JsonResult(await pandaScoreResultsService.GetLiveMatchesAsync(EventId, Stage));
	}
}
