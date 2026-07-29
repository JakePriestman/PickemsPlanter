using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using PickemsPlanter.APIs;
using PickemsPlanter.Models.Configurations;
using PickemsPlanter.Services;
using System.Security.Claims;
using System.Text.Json;

namespace PickemsPlanter.Extensions;

public static class ServiceCollectionExtensions
{
	extension (IServiceCollection services)
	{
		public void ConfigureServices(IConfiguration config)
		{
			services.AddAuth(config);
			services.AddCachingServices();
			services.AddJsonSerialization();
			services.AddHttpClients(config);
			services.AddTableStorage(config);

			services.AddRazorPages();
			services.AddSingleton<IPickemsService, PickemsService>();
			services.AddSingleton<ISeedsService, SeedsService>();
			services.AddSingleton<IPandaScoreResultsService, PandaScoreResultsService>();
			services.AddSingleton<ICoinProgressService, CoinProgressService>();
			services.AddSingleton<ISwissStandingsCalculator, SwissStandingsCalculator>();
			services.AddSingleton<IAdvancingSeedAutomationService, AdvancingSeedAutomationService>();
			services.AddSingleton<IHltvRankingParser, HltvRankingParser>();
			services.AddSingleton<IStageRosterService, StageRosterService>();
			services.AddOptions<SteamConfig>().Bind(config.GetSection(nameof(SteamConfig)));
			services.AddOptions<PandaScoreConfig>().Bind(config.GetSection(nameof(PandaScoreConfig)));
			services.AddOptions<EventDiscoveryConfig>().Bind(config.GetSection(nameof(EventDiscoveryConfig)));
			services.AddOptions<AdminConfig>().Bind(config.GetSection(nameof(AdminConfig)));
		}
		public void AddAuth(IConfiguration config)
		{
			services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
					.AddCookie(options =>
					{
						options.LoginPath = "/Login";
					});

			// The admin's Steam ID is Key Vault-backed (AdminConfig--SteamId), same as
			// SteamConfig.WebApiKey/PandaScoreConfig.ApiToken — never committed.
			string? adminSteamId = config["AdminConfig:SteamId"];

			services.AddAuthorization(options => options.AddPolicy("AdminOnly",
				policy => policy.RequireAssertion(ctx =>
					adminSteamId is not null && ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value == adminSteamId)));
		}

		public void AddCachingServices()
		{
			services.AddMemoryCache();
			services.AddSingleton<IUserPredictionsCachingService, UserPredictionsCachingService>();
			services.AddSingleton<ITournamentCachingService, TournamentCachingService>();
			services.AddHostedService<StartupCachingService>();

			services.AddSingleton<PandaScoreResultsCachingService>();
			services.AddSingleton<IPandaScoreResultsCachingService>(sp => sp.GetRequiredService<PandaScoreResultsCachingService>());
			services.AddHostedService(sp => sp.GetRequiredService<PandaScoreResultsCachingService>());

			services.AddHostedService<SteamEventDiscoveryService>();
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

			string? pandaScoreAPIURL = config["PandaScore:ApiUrl"];

			services.AddHttpClient<IPandaScoreApi, PandaScoreAPI>(opt => opt.BaseAddress = new Uri(pandaScoreAPIURL!));
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
