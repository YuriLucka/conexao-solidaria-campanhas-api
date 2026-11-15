using Campanhas.Domain.Exceptions;

namespace Campanhas.Domain.Entities;

// Sessão renovável de um usuário. O token em si nunca é gravado, só o hash SHA-256.
// Cada renovação troca o token por um novo da mesma família (rotação). Um token já trocado que volta a aparecer
// fora da tolerância indica cópia do token, e a família inteira é revogada.
public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid Familia { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }
    public DateTime ExpiraEm { get; private set; }
    public DateTime? SubstituidoEm { get; private set; }
    public DateTime? RevogadoEm { get; private set; }

    // EF Core
    protected RefreshToken() { }

    public RefreshToken(Guid usuarioId, Guid familia, string tokenHash, DateTime agora, DateTime expiraEm)
    {
        if (usuarioId == Guid.Empty || familia == Guid.Empty)
            throw new DomainException("Sessão inválida.");
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Hash do token não pode ser vazio.");
        if (expiraEm <= agora)
            throw new DomainException("A validade da sessão precisa estar no futuro.");

        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        Familia = familia;
        TokenHash = tokenHash;
        CriadoEm = agora;
        ExpiraEm = expiraEm;
    }

    public bool Ativo(DateTime agora) => RevogadoEm is null && SubstituidoEm is null && ExpiraEm > agora;

    // Trocado há instantes por outra aba ou requisição simultânea: ainda pode gerar um novo token da mesma família.
    public bool TrocadoHaPouco(DateTime agora, TimeSpan tolerancia)
        => RevogadoEm is null && SubstituidoEm is { } trocadoEm && agora - trocadoEm <= tolerancia && ExpiraEm > agora;

    public void MarcarSubstituido(DateTime agora) => SubstituidoEm ??= agora;

    public void Revogar(DateTime agora) => RevogadoEm ??= agora;
}
