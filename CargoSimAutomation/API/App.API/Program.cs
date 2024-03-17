using App.Core.Clients;
using App.Core.Hubs;
using App.Core.Services;
using App.Domain.DTOs;
using App.Domain.Services;
using Newtonsoft.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

//Appsettings properties
IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

// CORS config
builder.Services.AddCors(options => options.AddPolicy("CorsPolicy", builder =>
{
    builder
        .SetIsOriginAllowed((host) => true)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
}));

// Clients config
HttpClientHandler clientHandler = new HttpClientHandler();
clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }; //Bypass Docker ssl

builder.Services.AddHttpClient<HahnCargoSimClient>("DockerBypassSsl")
    .ConfigurePrimaryHttpMessageHandler(() => clientHandler);

builder.Services.AddSingleton(x => new HahnCargoSimClient(x.GetRequiredService<IHttpClientFactory>(), configuration));

// RabbitMQ - Consumer
builder.Services.AddSingleton(x => new Consumer(configuration));

// Add services
builder.Services.AddTransient<AuthDto>();
builder.Services.AddSingleton<AutomationHub>();

builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<IOrderService, OrderService>();
builder.Services.AddTransient<ISimulationService, SimulationService>();
builder.Services.AddTransient<IAutomation, Automation>();

builder.Services.AddSingleton(configuration);

builder.Services.AddSignalR();

builder.Services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                });

var app = builder.Build();


app.UseCors("CorsPolicy");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHub<AutomationHub>("/AutomationHub");

app.Run();
