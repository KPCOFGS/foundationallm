using FoundationaLLM.Chat.Helpers;
using FoundationaLLM.Common.Services;
using FoundationaLLM.Core.Interfaces;
using FoundationaLLM.Core.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient(FoundationaLLM.Common.Constants.HttpClients.DefaultHttpClient,
        httpClient =>
        {
            httpClient.BaseAddress = new Uri(builder.Configuration["FoundationaLLM:ChatManager:APIUrl"]);
        })
    .AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.WaitAndRetryAsync(
            3, retryNumber => TimeSpan.FromMilliseconds(600)));

var keyVaultUri = builder.Configuration["FoundationaLLM:Configuration:KeyVaultUri"];
builder.Services.AddSingleton<FoundationaLLM.Common.Interfaces.IConfiguration>(new KeyVaultConfiguration(keyVaultUri));

var serviceProvider = builder.Services.BuildServiceProvider();
var kvConfig = serviceProvider.GetRequiredService<FoundationaLLM.Common.Interfaces.IConfiguration>();

var azureAdSecret = kvConfig.GetValue(builder.Configuration["FoundationaLLM:Entra:ClientSecretKeyName"]);
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        options.ClientSecret = azureAdSecret;
        options.Instance = builder.Configuration["FoundationaLLM:Entra:Instance"];
        options.TenantId = builder.Configuration["FoundationaLLM:Entra:TenantId"];
        options.ClientId = builder.Configuration["FoundationaLLM:Entra:ClientId"];
        //options.Scope.Add(builder.Configuration["FoundationaLLM:Entra:Scopes"]);
        options.CallbackPath = builder.Configuration["FoundationaLLM:Entra:CallbackPath"];
    });
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();
builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.RegisterConfiguration();
builder.Services.AddRazorPages();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddServerSideBlazor()
    .AddMicrosoftIdentityConsentHandler();
builder.Services.RegisterServices();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();

static class ProgramExtensions
{
    public static void RegisterConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<ChatManagerSettings>()
            .Bind(builder.Configuration.GetSection("FoundationaLLM:ChatManager"));
    }

    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<IChatManager, ChatManager>();
    }
}
