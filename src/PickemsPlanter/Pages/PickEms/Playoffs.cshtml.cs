using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PickemsPlanter.Services;

namespace PickemsPlanter.Pages.PickEms;


public class PlayoffsModel(IPickemsService pickemsService, IHttpContextAccessor httpContextAccessor, IUserPredictionsCachingService cachingService) : PageModel
{
	[BindProperty(SupportsGet = true)]
	public required string EventId { get; init; }

	[BindProperty(SupportsGet = true)]
	public required string EventName { get; init; }

	[BindProperty(SupportsGet = true)]
	public required string SteamId { get; init; }

	[BindProperty(SupportsGet = true)]
	public string? SelectedEvent { get; init; }

	public IReadOnlyCollection<string>? Picks { get; set; }

	public required string? PersonaName = httpContextAccessor?.HttpContext?.User.FindFirst("PersonaName")?.Value;

	public required string? Avatar = httpContextAccessor?.HttpContext?.User.FindFirst("Avatar")?.Value;

	public async Task<JsonResult> OnGetPicksAllowed()
	{
		bool picksAllowed = await pickemsService.GetPlayoffsPicksAllowedAsync(EventId);
		return new JsonResult(picksAllowed);
	}

	public async Task<JsonResult> OnGetImages()
    {
		return new JsonResult(await pickemsService.GetTeamsInPlayoffsAsync(EventId));
    }

    public async Task<JsonResult> OnGetPicks()
    {
		return new JsonResult(await pickemsService.GetPlayoffPicksAsync(SteamId, EventId));
    }

	public async Task<JsonResult> OnGetResults()
	{
		return new JsonResult(await pickemsService.GetPlayoffResultsAsync(EventId));
	}

	public async Task<IActionResult> OnPostSendPicks(string picksToPost)
	{
		var authCode = await cachingService.GetAuthCodeFromCacheAsync(EventId, SteamId);

		await pickemsService.PostPlayoffPickemsAsync(picksToPost, SteamId, EventId, authCode);

		return RedirectToPage("/PickEms/Playoffs", new
		{
			EventId,
			EventName,
			SteamId
		});
	}
}
