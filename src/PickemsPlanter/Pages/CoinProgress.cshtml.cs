using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PickemsPlanter.Models.CoinProgress;
using PickemsPlanter.Services;

namespace PickemsPlanter.Pages;

public class CoinProgressModel(ICoinProgressService coinProgressService) : PageModel
{
	[BindProperty(SupportsGet = true)]
	public required string EventId { get; init; }

	[BindProperty(SupportsGet = true)]
	public required string SteamId { get; init; }

	public CoinProgressResult Progress { get; private set; } = null!;

	public async Task OnGetAsync()
	{
		Progress = await coinProgressService.GetCoinProgressAsync(SteamId, EventId);
	}
}
