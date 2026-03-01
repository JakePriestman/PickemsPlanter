using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using PickemsPlanter.APIs;
using PickemsPlanter.Models.Configurations;
using PickemsPlanter.Services;
using System.Text.Json;

namespace PickemsPlanter.Extensions;

public static class ServiceCollectionExtensions
{
	extension (IServiceCollection services)
	{
		public void ConfigureServices(IConfiguration config)
		{
			services.AddAuth();
			services.AddCachingServices();
			services.AddJsonSerialization();
			services.AddHttpClients(config);
			services.AddTableStorage(config);

			services.AddRazorPages();
			services.AddSingleton<IPickemsService, PickemsService>();
			services.AddSingleton<ISeedsService, SeedsService>();
			services.AddOptions<SteamConfig>().Bind(config.GetSection(nameof(SteamConfig)));
		}
		public void AddAuth()
		{
			services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
					.AddCookie(options =>
					{
						options.LoginPath = "/Login";
					});
		}

		public void AddCachingServices()
		{
			services.AddMemoryCache();
			services.AddSingleton<IUserPredictionsCachingService, UserPredictionsCachingService>();
			services.AddSingleton<ITournamentCachingService, TournamentCachingService>();
			services.AddHostedService<StartupCachingService>();
		}

		public void AddJsonSerialization()
		{
			services.AddSingleton<JsonSerializerOptions>(_ => new()
			{
				PropertyNameCaseInsensitive = true
			});
		}

		public void AddHttpClients(IConfiguration config)
		{
			string? steamAPIURL = config["Steam:APIURL"];

			services.AddHttpClient<ISteamAPI, SteamAPI>(opt => opt.BaseAddress = new Uri(steamAPIURL!));

			string? steamOpenIDURL = config["Steam:OpenIDURL"];

			services.AddHttpClient<ILoginAPI, LoginAPI>(opt => opt.BaseAddress = new Uri(steamOpenIDURL!));
		}

		public void AddTableStorage(IConfiguration config)
		{
			string? tableStorageUrl = config["TableStorage:URL"];

			if (tableStorageUrl is not null)
				services.AddSingleton(new TableServiceClient(new Uri(tableStorageUrl), new DefaultAzureCredential()));

			services.AddSingleton<IUserEventsTableService, UserEventsTableService>();

			services.AddSingleton<ISeedsTableService, SeedsTableService>();
			services.AddSingleton<IEventTableService, EventTableService>();
		}
	}
}
