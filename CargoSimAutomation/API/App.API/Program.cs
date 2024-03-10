using App.Core.Clients;
using App.Core.Hubs;
using App.Core.Services;
using App.Domain.DTOs;
using App.Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using QuickGraph.Algorithms.Services;

var builder = WebApplication.CreateBuilder(args);

//Appsettings properties
IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

// CORS config
builder.Services.AddCors(options => options.AddPolicy("CorsPolicy", builder =>
{
    builder
    .AllowAnyMethod()
    .AllowAnyHeader()
    .WithOrigins(configuration.GetSection("Cors").Value)
    .AllowCredentials();
}));

// Clients config

builder.Services.AddHttpClient();

builder.Services.AddSingleton(x => new HahnCargoSimClient(x.GetRequiredService<IHttpClientFactory>()));

// RabbitMQ - Consumer

builder.Services.AddSingleton(x => new Consumer());

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


// Configure the HTTP request pipeline.

app.UseCors("CorsPolicy");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHub<AutomationHub>("/AutomationHub");

app.Run();
