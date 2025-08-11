using Azure.Data.Tables;
using CS2Pickems.Models.Steam;
using CS2Pickems.Models.TableStorage;
using CS2Pickems.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace CS2Pickems.Pages.PickEms
{
    public class AddAuthCodeModel(ITableStorageService storageService, IUserPredictionsCachingService cachingService, IMemoryCache memoryCache) : PageModel
    {
		[BindProperty(SupportsGet = true)]
		public required string EventId { get; set; }

		[BindProperty(SupportsGet = true)]
        public required string EventName { get; set; }

		[BindProperty(SupportsGet = true)]
		public required string SteamId { get; set; }

		[BindProperty]
		[DataType(DataType.Password)]
		public string AuthCode { get; set; }

		private readonly ITableStorageService _storageService = storageService;

		private readonly IUserPredictionsCachingService _cachingService = cachingService;

		private readonly IMemoryCache _memoryCache = memoryCache;

		public async Task<IActionResult> OnPost()
        {
            UserEvent userEvent = new()
            {
                PartitionKey = SteamId,
                RowKey = EventId,
                AuthCode = AuthCode
            };

			try
			{
				await _storageService.CreateEntryAsync(userEvent);

				await _cachingService.RefreshUserPredictionsAsync(SteamId, EventId, AuthCode);

				_memoryCache.Set($"TOURNAMENT_{EventId}_USER_{SteamId}_AUTHCODE", AuthCode);

				return RedirectToPage("/PickEms/Stage1", new
				{
					eventId = EventId,
					eventName = EventName,
					steamId = SteamId
				});
			}
			catch (Azure.RequestFailedException)
			{
				return Redirect("/Error");
			}
        }
    }
}
