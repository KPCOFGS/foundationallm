using Azure.Identity;
using FoundationaLLM.Core.Models.Configuration;

using CoreWorkerService;
using FoundationaLLM.Core.Interfaces;
using FoundationaLLM.Core.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("appsettings.json", false, true);
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddJsonFile("appsettings.development.json", true, true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Connect(builder.Configuration["FoundationaLLM:AppConfig:ConnectionString"]);
#if DEBUG
    var credential = new DefaultAzureCredential();
#else
    var credential = new ChainedTokenCredential(
            new ManagedIdentityCredential(clientId),
            new AzureCliCredential()
        );
#endif
    options.ConfigureKeyVault(options =>
    {
        options.SetCredential(credential);
    });
});
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddJsonFile("appsettings.development.json", true, true);
builder.Services.AddOptions<CosmosDbSettings>()
    .Bind(builder.Configuration.GetSection("FoundationaLLM:CosmosDB"));
builder.Services.AddSingleton<ICosmosDbService, CosmosDbService>();
builder.Services.AddSingleton<ICosmosDbChangeFeedService, CosmosDbChangeFeedService>();
builder.Services.AddHostedService<ChangeFeedWorker>();

var host = builder.Build();

host.Run();