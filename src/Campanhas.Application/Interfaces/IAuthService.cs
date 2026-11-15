using Campanhas.Application.DTOs;

namespace Campanhas.Application.Interfaces;

public interface IAuthService
{
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
