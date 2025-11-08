using PickemsPlanter.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfigurationBuilder configBuilder = builder.Configuration;
IConfigurationRoot config = configBuilder.Build();
configBuilder.AddKeyVault(config);

IServiceCollection services = builder.Services;
services.ConfigureServices(config);
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/", context => {
	context.Response.Redirect("/Profile/Login");
	return Task.CompletedTask;
});

app.Run();