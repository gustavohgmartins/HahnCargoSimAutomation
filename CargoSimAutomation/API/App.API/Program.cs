using App.Core.Clients;
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
    .AllowAnyMethod()
    .AllowAnyHeader()
    .WithOrigins(configuration.GetSection("Cors").Value)
    .AllowCredentials();
}));

// Clients config

builder.Services.AddHttpClient();

//builder.Services.AddHttpClient(HahnCargoSimClient.Name, configure =>
//{
//    configure.BaseAddress = new System.Uri(configuration.GetSection("Clients:HahnCargoSimEndpoint").Value);
//});

builder.Services.AddSingleton(x => new HahnCargoSimClient(x.GetRequiredService<IHttpClientFactory>()));

// RabbitMQ - Consumer

builder.Services.AddSingleton(x => new Consumer());

// Add services

builder.Services.AddTransient<AuthDto>();
builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddTransient<ISimulationService, SimulationService>();
builder.Services.AddTransient<IAutomation, Automation>();

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

app.Run();
