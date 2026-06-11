using PickemsPlanter.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PickemsPlanter.Models.Event;

namespace PickemsPlanter.Pages.PickEms;

public class StageModel(IPickemsService pickemsService, IHttpContextAccessor httpContextAccessor, IUserPredictionsCachingService cachingService) : PageModel
{
	[BindProperty(SupportsGet = true)]
	public required string EventId { get; init; }

	[BindProperty(SupportsGet = true)]
	public required string EventName { get; init; }

	[BindProperty(SupportsGet = true)]
	public required string SteamId { get; init; }

    [BindProperty(SupportsGet = true)]
    public required Stages Stage { get; init; }

	[BindProperty(SupportsGet = true)]
	public string? SelectedEvent { get; init; }

	public IReadOnlyCollection<string>? Picks { get; set; }

    public required string? PersonaName = httpContextAccessor?.HttpContext?.User.FindFirst("PersonaName")?.Value;

    public required string? Avatar = httpContextAccessor?.HttpContext?.User.FindFirst("Avatar")?.Value;

	public async Task<JsonResult> OnGetPicksAllowed()
	{
		bool picksAllowed = await pickemsService.GetStagePicksAllowedAsync(Stage, EventId);

		return new JsonResult(picksAllowed);
	}

	public async Task<JsonResult> OnGetTeamNames()
	{
		return new JsonResult(await pickemsService.GetTeamNameMapAsync(EventId));
	}

	public async Task<JsonResult> OnGetImages()
    {
        return new JsonResult(await pickemsService.GetTeamsInStageAsync(Stage, EventId));
    }

    public async Task<JsonResult> OnGetPicks()
    {
		return new JsonResult(await pickemsService.GetStagePicksAsync(Stage, SteamId, EventId));
	}

    public async Task<JsonResult> OnGetResults()
    {
        return new JsonResult(await pickemsService.GetStageResultsAsync(Stage, EventId));
	}

    public async Task<IActionResult> OnPostSendPicks(string picksToPost)
    {
		var authCode = await cachingService.GetAuthCodeFromCacheAsync(EventId, SteamId);

        await pickemsService.PostStagePickemsAsync(Stage, picksToPost, SteamId, EventId, authCode);

		return RedirectToPage("/PickEms/Stage", new
		{
			EventId,
			EventName,
			SteamId,
			stage = Stage
		});
	}
}
