using System.Text.Json;
using Campanhas.Application.DTOs;
using Campanhas.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Campanhas.Infrastructure.Cache;

// Cache-aside do painel público. Chave compartilhada com o doacoes-worker, que a apaga após cada doação processada.
// Redis fora do ar não derruba a API: registra o aviso e segue para o banco.
public class RedisCacheCampanhasAtivas : ICacheCampanhasAtivas
{
    public const string Chave = "campanhas:ativas";
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheCampanhasAtivas> _logger;

    public RedisCacheCampanhasAtivas(IDistributedCache cache, ILogger<RedisCacheCampanhasAtivas> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CampanhaAtivaResponse>?> ObterAsync(CancellationToken ct = default)
    {
        try
        {
            var bytes = await _cache.GetAsync(Chave, ct);
            return bytes is null ? null : JsonSerializer.Deserialize<List<CampanhaAtivaResponse>>(bytes, Json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Redis indisponível ao ler {Chave}; lendo do banco.", Chave);
            return null;
        }
    }

    public async Task DefinirAsync(IReadOnlyList<CampanhaAtivaResponse> campanhas, CancellationToken ct = default)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(campanhas, Json);
            await _cache.SetAsync(Chave, bytes, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl }, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Redis indisponível ao gravar {Chave}.", Chave);
        }
    }

    public async Task InvalidarAsync(CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(Chave, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Redis indisponível ao invalidar {Chave}.", Chave);
        }
    }
}
