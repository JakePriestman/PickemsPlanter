using Azure.Data.Tables;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using CS2Pickems.APIs;
using CS2Pickems.Models.Configurations;
using CS2Pickems.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Json;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

DefaultAzureCredential credential = new();

AddKeyVault(builder.Configuration, credential);

ConfigureServices(builder.Services, builder.Configuration, credential);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/", context => {
	context.Response.Redirect("/Login");
	return Task.CompletedTask;
});

app.Run();


static void AddKeyVault(IConfigurationBuilder builder, DefaultAzureCredential credential)
{
	IConfigurationRoot config = builder.Build();
	string? keyVaultURI = config["KeyVault:URL"];

	if (keyVaultURI is not null)
	{
		builder.AddAzureKeyVault(new SecretClient(new Uri(keyVaultURI), credential), new KeyVaultSecretManager());
	}
}

static void ConfigureServices(IServiceCollection services, IConfigurationBuilder builder, DefaultAzureCredential credential)
{
	IConfigurationRoot config = builder.Build();

	services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
				.AddCookie(options =>
				{
					options.LoginPath = "/Login"; // Path to your login page
												  //options.AccessDeniedPath = "/Account/AccessDenied"; // Path for access denied
				});

	services.AddRazorPages();

	services.AddSingleton<JsonSerializerOptions>(_ => new()
	{
		PropertyNameCaseInsensitive = true
	});

	services.AddMemoryCache();

	string? steamAPIURL = config["Steam:APIURL"];

	services.AddHttpClient<ISteamAPI, SteamAPI>(opt => opt.BaseAddress = new Uri(steamAPIURL!));

	string? steamOpenIDURL = config["Steam:OpenIDURL"];

	services.AddHttpClient<ILoginAPI, LoginAPI>(opt => opt.BaseAddress = new Uri(steamOpenIDURL!));

	services.AddSingleton<IPickemsService, PickemsService>();

	services.AddSingleton<IUserPredictionsCachingService, UserPredictionsCachingService>();

	services.AddSingleton<ITournamentCachingService, TournamentCachingService>();

	string? tableStorageUrl = config["TableStorage:URL"];

	if (tableStorageUrl is not null)
		services.AddSingleton(new TableServiceClient(new Uri(tableStorageUrl), credential));

	services.AddSingleton<ITableStorageService, TableStorageService>();

	services.AddOptions<SteamConfig>().Bind(config.GetSection(nameof(SteamConfig)));

	services.AddHostedService<StartupCachingService>();
}
