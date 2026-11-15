using Campanhas.Application.DTOs;
using Campanhas.Domain.Entities;

namespace Campanhas.Application.Interfaces;

public interface IJwtTokenService
{
    TokenResponse Gerar(Usuario usuario);
}
