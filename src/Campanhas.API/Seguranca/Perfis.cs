using Campanhas.Domain.Enums;

namespace Campanhas.API.Seguranca;

// Nomes dos perfis para [Authorize(Roles = ...)], iguais aos valores do enum gravados na claim "role".
public static class Perfis
{
    public const string GestorONG = nameof(Perfil.GestorONG);
    public const string Doador = nameof(Perfil.Doador);
}
