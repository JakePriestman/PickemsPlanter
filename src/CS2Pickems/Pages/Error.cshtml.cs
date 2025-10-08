using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CS2Pickems.Pages
{
    public class ErrorModel(ILogger<ErrorModel> logger) : PageModel
    {
		private readonly ILogger<ErrorModel> _logger = logger;

		public string? RequestId { get; set; }

		public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

		public void OnGet()
		{
			RequestId = HttpContext.TraceIdentifier;
		}
	}
}
