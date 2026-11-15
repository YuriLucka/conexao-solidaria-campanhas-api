using System.Text;
using System.Text.Json.Serialization;
using Campanhas.API.Middleware;
using Campanhas.API.OpenApi;
using Campanhas.Application.Interfaces;
using Campanhas.Application.Services;
using Campanhas.Infrastructure;
using Campanhas.Infrastructure.Data;
using Campanhas.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
});

// Status e perfis trafegam como texto ("Ativa", "GestorONG"), como no enunciado.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ErrorHandler>();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDoadorService, DoadorService>();
builder.Services.AddScoped<ICampanhaService, CampanhaService>();
builder.Services.AddScoped<IDoacaoService, DoacaoService>();

var jwt = builder.Configuration.GetSection(JwtSettings.Secao).Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwt.SecretKey))
    throw new InvalidOperationException("JwtSettings:SecretKey não configurado.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Mantém os nomes curtos das claims ("sub", "role") em vez de mapear para os URIs do .NET.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            NameClaimType = "name",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

// Front (Blazor WASM) em outra origem durante o desenvolvimento; atrás do Kong fica na mesma origem.
var origens = (builder.Configuration["Cors:Origens"] ?? "http://localhost:5173,http://localhost:8080")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddOpenApi(o =>
{
    o.AddDocumentTransformer<DocumentoTransformer>();
    o.AddOperationTransformer<SegurancaOperacaoTransformer>();
});

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("postgres");

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseRouting();
app.UseHttpMetrics();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Documentação sempre ligada: é a interface usada na correção e no vídeo.
app.MapOpenApi();
app.MapScalarApiReference(o => o
    .WithTitle("Conexão Solidária - Campanhas API")
    .AddPreferredSecuritySchemes("Bearer")
    .DisableDefaultFonts());

// /health: pronto para receber tráfego (inclui o PostgreSQL). /health/live: processo de pé.
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthResponse.EscreverAsync });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false, ResponseWriter = HealthResponse.EscreverAsync });
app.MapMetrics();

await PrepararBancoAsync(app);

app.Run();

// Aplica as migrações (com retry enquanto o PostgreSQL sobe) e o seed de demonstração, se ligado.
static async Task PrepararBancoAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    for (var tentativa = 1; ; tentativa++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (tentativa < 30)
        {
            logger.LogWarning("Banco indisponível (tentativa {Tentativa}/30): {Erro}", tentativa, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    if (app.Configuration.GetValue<bool>("Seed:Enabled"))
    {
        var hasher = scope.ServiceProvider.GetRequiredService<ISenhaHasher>();
        var agora = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        await DbSeeder.SeedAsync(db, hasher, agora);
    }
}

public partial class Program { } // usado pelos testes de integração
