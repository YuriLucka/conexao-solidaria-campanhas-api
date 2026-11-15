using Campanhas.Domain.Entities;

namespace Campanhas.Domain.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExisteAsync(string email, CancellationToken ct = default);
    Task<bool> CpfExisteAsync(string cpf, CancellationToken ct = default);
    Task AdicionarAsync(Usuario usuario, CancellationToken ct = default);
}
