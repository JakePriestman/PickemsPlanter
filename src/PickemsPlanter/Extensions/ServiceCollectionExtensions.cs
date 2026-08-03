using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using PickemsPlanter.APIs;
using PickemsPlanter.Models.Configurations;
using PickemsPlanter.Services;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;

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
			services.AddRateLimiting(config);

			services.AddRazorPages();
			services.AddSingleton<IPickemsService, PickemsService>();
			services.AddSingleton<ISeedsService, SeedsService>();
			services.AddSingleton<IPandaScoreResultsService, PandaScoreResultsService>();
			services.AddSingleton<ICoinProgressService, CoinProgressService>();
			services.AddSingleton<ILeaderboardService, LeaderboardService>();
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

		// Bounds how hard a traffic spike can hammer the Steam Web API key, which is
		// shared across every user — a burned/rate-limited key breaks the app for
		// everyone, not just the caller responsible. "SteamApi" covers the PickEms
		// read/write handlers; "Auth" is tighter since the OpenID callback is
		// unauthenticated and only needs to fire once per real login.
		public void AddRateLimiting(IConfiguration config)
		{
			services.AddOptions<RateLimitingConfig>().Bind(config.GetSection(nameof(RateLimitingConfig)));

			RateLimitingConfig rateLimiting = config.GetSection(nameof(RateLimitingConfig)).Get<RateLimitingConfig>() ?? new();

			services.AddRateLimiter(options =>
			{
				options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

				options.AddPolicy("SteamApi", httpContext => RateLimitPartition.GetFixedWindowLimiter(
					partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
					factory: _ => new FixedWindowRateLimiterOptions
					{
						PermitLimit = rateLimiting.SteamApiPermitLimit,
						Window = TimeSpan.FromMinutes(rateLimiting.SteamApiWindowMinutes),
						QueueLimit = 0
					}));

				options.AddPolicy("Auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
					partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
					factory: _ => new FixedWindowRateLimiterOptions
					{
						PermitLimit = rateLimiting.AuthPermitLimit,
						Window = TimeSpan.FromMinutes(rateLimiting.AuthWindowMinutes),
						QueueLimit = 0
					}));
			});
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
