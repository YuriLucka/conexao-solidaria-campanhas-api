using Campanhas.Application.DTOs;

namespace Campanhas.Application.Interfaces;

public interface IAuthService
{
    Task<SessaoAutenticada> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<SessaoAutenticada> RenovarAsync(string? refreshToken, CancellationToken ct = default);
    Task SairAsync(string? refreshToken, CancellationToken ct = default);
}
