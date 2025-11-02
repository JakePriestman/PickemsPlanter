using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CS2Pickems.Pages
{
    public class ErrorModel(IHttpContextAccessor httpContextAccessor) : PageModel
    {
		public string? RequestId { get; set; }

		public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

		private bool? IsAuthenticated => httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated;

		public void OnGet()
		{
			RequestId = HttpContext.TraceIdentifier;
		}

		public IActionResult OnPostOverview()
		{
			if (IsAuthenticated is false)
				return RedirectToPage("/Profile/Login");
			else 
				return RedirectToPage("/Profile/Overview");
		}
	}
}
