using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Models.TableStorage;
using PickemsPlanter.Services;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PickemsPlanter.Pages.Profile;

public class OverviewModel(IUserEventsTableService tableStorageService, IUserPredictionsCachingService cachingService, ITournamentCachingService tournamentCachingService, IMemoryCache memoryCache, IHttpContextAccessor httpContextAccessor,
	IEventTableService eventTableService, ICoinProgressService coinProgressService) : PageModel
{
	[BindProperty]
	public string SelectedEvent { get; set; } = string.Empty;

	public required string? PersonaName = httpContextAccessor?.HttpContext?.User.FindFirst("PersonaName")?.Value;

	public required string? Avatar = httpContextAccessor?.HttpContext?.User.FindFirst("Avatar")?.Value;

	public required string SteamId = httpContextAccessor?.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

	public List<SelectListItem> EventOptions { get; set; } = [];

	[BindProperty]
	public Dictionary<string, string> AuthCodes { get; set; } = [];

	private const string FAKE_AUTH_CODE = "FAKE_AUTH_CODE";

	public async Task OnGetAsync()
	{
		await LoadEventsAsync();

		AuthCodes = EventOptions.ToDictionary(key => key.Value, value => string.Empty);

		foreach (var key in AuthCodes.Keys)
		{
			bool tableEntityExists = await tableStorageService.ExistsAsync(SteamId, key);

			if (tableEntityExists)
				AuthCodes[key] = FAKE_AUTH_CODE;
		}
	}

	public async Task<IActionResult> OnPostChooseEvent()
	{
		await LoadEventsAsync();

		var eventName = EventOptions.First(x => x.Value == SelectedEvent).Text;

		var encodedName = WebUtility.UrlEncode(eventName);

		var authCode = AuthCodes[SelectedEvent];

		if (authCode == FAKE_AUTH_CODE)
		{
			UserEvent? userEvent = await tableStorageService.GetEntryIfExistsAsync(SteamId, SelectedEvent);	

			if (userEvent is null)
				ArgumentNullException.ThrowIfNull(userEvent);

			await CacheOnChooseEvent(userEvent.AuthCode);
		}

		else
		{
			await tableStorageService.CreateUserEventIfNotExistsAsync(SteamId, SelectedEvent, authCode);

			await CacheOnChooseEvent(authCode);
		}

		var firstActiveStage = await tournamentCachingService.GetFirstActiveStageOrDefaultAsync(SelectedEvent);

		if (firstActiveStage == Stages.Playoffs)
		{
			return RedirectToPage("/PickEms/Playoffs", new
			{
				eventId = SelectedEvent,
				eventName,
				SteamId
			});
		}

		return RedirectToPage("/PickEms/Stage", new
		{
			eventId = SelectedEvent,
			eventName,
			SteamId,
			stage = firstActiveStage
		});
	}

	public async Task<IActionResult> OnPostDelete()
	{
		await tableStorageService.DeleteEntityIfExistsAsync(SteamId, SelectedEvent);

		memoryCache.Remove($"TOURNAMENT_{SelectedEvent}_USER_{SteamId}_AUTHCODE");
		memoryCache.Remove($"USER_{SteamId}_TOURNAMENT_{SelectedEvent}_PICKS");

		return RedirectToPage("/Profile/Overview");
	}

	public async Task<IActionResult?> OnGetAuthCode(string eventId)
	{
		var authCode = await tableStorageService.GetEntryIfExistsAsync(SteamId, eventId);

		if (authCode == null) return NotFound();

		return new JsonResult(new { authCode.AuthCode });
	}

	// The app's default JsonResult serialization (camelCase, no enum converter) sends
	// CoinTier as its underlying int (0-3) rather than the name — fine for numeric fields,
	// but the frontend needs the tier by name ("Bronze"/"Silver"/...), not an ordinal that'd
	// silently break if the enum's declaration order ever changed.
	private static readonly JsonSerializerOptions CoinProgressJsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters = { new JsonStringEnumConverter() }
	};

	public async Task<JsonResult> OnGetCoinProgress(string eventId)
	{
		var progress = await coinProgressService.GetCoinProgressAsync(SteamId, eventId);

		return new JsonResult(progress, CoinProgressJsonOptions);
	}

	private async Task CacheOnChooseEvent(string authCode)
	{
		cachingService.CacheAuthCode(SelectedEvent, SteamId, authCode);

		await cachingService.RefreshUserPredictionsAsync(SteamId, SelectedEvent);

		await cachingService.CacheUserTeamsAsync(SteamId, SelectedEvent);

		memoryCache.Set($"TOURNAMENT_{SelectedEvent}_USER_{SteamId}_AUTHCODE", authCode);
	}

	private async Task LoadEventsAsync()
	{
		var events = await eventTableService.GetAllEventsAsync();

		EventOptions = [.. events.Where(x => !x.Disabled).Select(x => new SelectListItem
		{
			Text = x.Name,
			Value = x.Id
		})];
	}
}

