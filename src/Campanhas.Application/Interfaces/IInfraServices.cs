using Campanhas.Application.DTOs;
using Campanhas.Domain.Entities;

namespace Campanhas.Application.Interfaces;

public interface IJwtTokenService
{
    TokenResponse Gerar(Usuario usuario);
}

public interface ISenhaHasher
{
    string Hash(string senha);
    bool Verificar(string senha, string hash);
}

// Publica no broker. Com o outbox, o evento só sai depois do IUnitOfWork.SalvarAsync.
public interface IEventPublisher
{
    Task PublicarAsync<T>(T evento, CancellationToken ct = default) where T : class;
}

// Cache do painel público. Falha do cache nunca derruba a requisição.
public interface ICacheCampanhasAtivas
{
    Task<IReadOnlyList<CampanhaAtivaResponse>?> ObterAsync(CancellationToken ct = default);
    Task DefinirAsync(IReadOnlyList<CampanhaAtivaResponse> campanhas, CancellationToken ct = default);
    Task InvalidarAsync(CancellationToken ct = default);
}
