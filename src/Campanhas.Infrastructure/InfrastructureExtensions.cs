using Campanhas.Application.Interfaces;
using Campanhas.Domain.Interfaces;
using Campanhas.Infrastructure.Cache;
using Campanhas.Infrastructure.Consultas;
using Campanhas.Infrastructure.Data;
using Campanhas.Infrastructure.Messaging;
using Campanhas.Infrastructure.Repositories;
using Campanhas.Infrastructure.Security;
using ConexaoSolidaria.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Campanhas.Infrastructure;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var conexao = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(conexao))
            throw new InvalidOperationException("ConnectionStrings:Postgres não configurado.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(conexao));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ICampanhaRepository, CampanhaRepository>();
        services.AddScoped<IDoacaoRepository, DoacaoRepository>();
        services.AddScoped<IConsultas, ConsultasPostgres>();

        services.AddOptions<JwtSettings>().Bind(configuration.GetSection(JwtSettings.Secao))
            .Validate(s => s.SecretKey.Length >= 32, "JwtSettings:SecretKey precisa de pelo menos 32 caracteres.")
            .ValidateOnStart();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ISenhaHasher, BCryptSenhaHasher>();
        services.AddSingleton(TimeProvider.System);

        // Redis: sem prefixo (InstanceName vazio), porque o worker apaga a mesma chave.
        // AbortOnConnectFail=false: a API sobe e responde mesmo com o Redis fora.
        var redis = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redis))
            throw new InvalidOperationException("ConnectionStrings:Redis não configurado.");
        services.AddStackExchangeRedisCache(o =>
        {
            var opcoes = ConfigurationOptions.Parse(redis);
            opcoes.AbortOnConnectFail = false;
            opcoes.ConnectTimeout = 2000;
            opcoes.SyncTimeout = 2000;
            o.ConfigurationOptions = opcoes;
        });
        services.AddScoped<ICacheCampanhasAtivas, RedisCacheCampanhasAtivas>();

        // Outbox transacional: o evento é gravado no PostgreSQL junto com a doação e entregue ao RabbitMQ em seguida.
        services.AddConexaoMessaging(configuration, x =>
        {
            x.AddEntityFrameworkOutbox<AppDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
                o.QueryDelay = TimeSpan.FromSeconds(1);
            });
        });
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

        return services;
    }
}
