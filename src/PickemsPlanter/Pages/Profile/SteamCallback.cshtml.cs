using PickemsPlanter.APIs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace PickemsPlanter.Pages
{
	public class SteamCallbackModel(ILoginAPI loginAPI, ISteamAPI steamAPI) : PageModel
	{
		private readonly ILoginAPI _loginAPI = loginAPI;
		private readonly ISteamAPI _steamAPI = steamAPI;

		public async Task<IActionResult> OnGet()
		{
			var query = Request.Query;

			var response = await _loginAPI.ValidateLoginAsync(query);

			if (!response.Contains("is_valid:true"))
			{
				return RedirectToPage("/Error");
			}

			var claimedId = query["openid.claimed_id"].ToString();
			var match = Regex.Match(claimedId, @"https://steamcommunity.com/openid/id/(\d+)");

			if (!match.Success)
			{
				return RedirectToPage("/Error");
			}

			var steamId = match.Groups[1].Value;

			var userResponse = await _steamAPI.GetPlayerSummeries(steamId);

			var player = userResponse.Response.Players.First();

			List<Claim> claims =
			[
				new (ClaimTypes.NameIdentifier, steamId),
				new ("PersonaName", player.PersonaName),
				new ("Avatar", player.AvatarFull)
			];

			ClaimsIdentity identity = new (claims, CookieAuthenticationDefaults.AuthenticationScheme);
			ClaimsPrincipal principal = new (identity);

			await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

			return RedirectToPage("/Profile/Overview");
		}
	}
}
