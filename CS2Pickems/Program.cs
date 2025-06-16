using CS2Pickems.APIs;
using CS2Pickems.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder.Services);


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

app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/", context => {
	context.Response.Redirect("/PickEmStage1");
	return Task.CompletedTask;
});

app.Run();


static void ConfigureServices(IServiceCollection services)
{
	services.AddRazorPages();

	services.AddSingleton<JsonSerializerOptions>(_ => new()
	{
		PropertyNameCaseInsensitive = true
	});

	services.AddMemoryCache();

	services.AddHttpClient<ISteamAPI, SteamAPI>(opt => opt.BaseAddress = new Uri("https://api.steampowered.com"));

	services.AddHostedService<CachingService>();
}
