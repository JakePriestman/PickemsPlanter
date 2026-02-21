using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Rendering;
using PickemsPlanter.APIs;
using PickemsPlanter.Models.Configurations;
using PickemsPlanter.Models.Event;
using PickemsPlanter.Services;
using System.Text.Json;

namespace PickemsPlanter.Extensions
{
	public static class ServiceCollectionExtensions
	{
		public static void ConfigureServices(this IServiceCollection services, IConfigurationRoot config)
		{
			services.AddAuth();
			services.AddCachingServices();
			services.AddJsonSerialization();
			services.AddHttpClients(config);
			services.AddTableStorage(config);
			services.AddEvents();

			services.AddRazorPages();
			services.AddSingleton<IPickemsService, PickemsService>();
			services.AddSingleton<ISeedsService, SeedsService>();
			services.AddOptions<SteamConfig>().Bind(config.GetSection(nameof(SteamConfig)));
		}

		public static void AddEvents(this IServiceCollection services)
		{
			string eventsJson = File.ReadAllText("events.json");

			JsonSerializerOptions options = new ()
			{
				PropertyNameCaseInsensitive = true,
			};

			IReadOnlyCollection<Event>? events = JsonSerializer.Deserialize<IReadOnlyCollection<Event>>(eventsJson, options);

			if (events is not null)
			{
				List<SelectListItem> eventOptions = [.. events.Select(x => new SelectListItem(x.Name, x.Id))];
				services.AddSingleton(_ => eventOptions);
			}
		}

		public static void AddAuth(this IServiceCollection services)
		{
			services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
					.AddCookie(options =>
					{
						options.LoginPath = "/Login";
					});
		}

		public static void AddCachingServices(this IServiceCollection services)
		{
			services.AddMemoryCache();
			services.AddSingleton<IUserPredictionsCachingService, UserPredictionsCachingService>();
			services.AddSingleton<ITournamentCachingService, TournamentCachingService>();
			services.AddHostedService<StartupCachingService>();
			services.AddSingleton<ISeedsCachingService, SeedsCachingService>();
		}

		public static void AddJsonSerialization(this IServiceCollection services)
		{
			services.AddSingleton<JsonSerializerOptions>(_ => new()
			{
				PropertyNameCaseInsensitive = true
			});
		}

		public static void AddHttpClients(this IServiceCollection services, IConfigurationRoot config)
		{
			string? steamAPIURL = config["Steam:APIURL"];

			services.AddHttpClient<ISteamAPI, SteamAPI>(opt => opt.BaseAddress = new Uri(steamAPIURL!));

			string? steamOpenIDURL = config["Steam:OpenIDURL"];

			services.AddHttpClient<ILoginAPI, LoginAPI>(opt => opt.BaseAddress = new Uri(steamOpenIDURL!));
		}

		public static void AddTableStorage(this IServiceCollection services, IConfigurationRoot config)
		{
			string? tableStorageUrl = config["TableStorage:URL"];

			if (tableStorageUrl is not null)
				services.AddSingleton(new TableServiceClient(new Uri(tableStorageUrl), new DefaultAzureCredential()));

			services.AddSingleton<IUserEventsTableService, UserEventsTableService>();

			services.AddSingleton<ISeedsTableService, SeedsTableService>();
		}
	}
}
