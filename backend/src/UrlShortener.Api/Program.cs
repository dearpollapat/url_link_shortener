using System.Text.Json.Serialization;
using UrlShortener.Api.Core;
using UrlShortener.Api.Core.Persistence;
using UrlShortener.Api.Core.ShortCodes;
using UrlShortener.Api.Core.Validation;
using UrlShortener.Api.Features.Links;
using UrlShortener.Api.Features.Redirect;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCors = "frontend";

// --- Configuration ---
builder.Services.Configure<ShortUrlOptions>(
    builder.Configuration.GetSection(ShortUrlOptions.SectionName));

// --- Core services ---
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ILinkRepository, InMemoryLinkRepository>();
builder.Services.AddSingleton<IUrlValidator, UrlValidator>();
builder.Services.AddSingleton<IPlatformDetector, UserAgentPlatformDetector>();

// Short-code generators. Registration order defines resolution priority:
// specific strategies first, the catch-all random generator last.
builder.Services.AddSingleton<IShortCodeGenerator, CustomAliasGenerator>();
builder.Services.AddSingleton<IShortCodeGenerator, RandomShortCodeGenerator>();
builder.Services.AddSingleton<ShortCodeGeneratorResolver>();

builder.Services.AddScoped<LinkService>();

// Serialize/deserialize enums as strings (e.g. "Disabled", "ios") for a friendlier contract.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// --- Error handling ---
// Business errors are returned as Result/Error and mapped to ProblemDetails at
// the endpoints. This only covers unexpected exceptions -> a 500 ProblemDetails.
builder.Services.AddProblemDetails();

// --- API surface ---
builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddPolicy(FrontendCors, policy =>
    policy.WithOrigins(
            builder.Configuration.GetValue<string>("Frontend:Origin") ?? "http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors(FrontendCors);

app.MapLinksEndpoints();
app.MapRedirectEndpoints();

app.Run();

// Exposes the implicit Program class to the test project (WebApplicationFactory).
public partial class Program;
