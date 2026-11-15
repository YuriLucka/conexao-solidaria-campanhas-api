using Campanhas.Domain.Enums;
using Campanhas.Domain.Exceptions;

namespace Campanhas.Domain.Entities;

// Intenção de doação. Nasce Pendente; o doacoes-worker muda para Processada ou Rejeitada.
public class Doacao
{
    public Guid Id { get; private set; }
    public Guid CampanhaId { get; private set; }
    public Guid DoadorId { get; private set; }
    public decimal Valor { get; private set; }
    public StatusDoacao Status { get; private set; }
    public string? MotivoRejeicao { get; private set; }
    public DateTime CriadaEm { get; private set; }
    public DateTime? ProcessadaEm { get; private set; }

    // EF Core
    protected Doacao() { }

    public Doacao(Campanha campanha, Guid doadorId, decimal valor, DateTime agora)
    {
        ArgumentNullException.ThrowIfNull(campanha);

        if (doadorId == Guid.Empty)
            throw new DomainException("Doador inválido.");
        if (valor <= 0)
            throw new DomainException("O valor da doação deve ser maior que zero.");
        if (valor > 999_999_999.99m)
            throw new DomainException("O valor da doação excede o limite permitido (R$ 999.999.999,99).");
        if (decimal.Round(valor, 2) != valor)
            throw new DomainException("O valor da doação deve ter no máximo duas casas decimais.");
        if (!campanha.AceitaDoacao(agora))
            throw new RegraNegocioException("Esta campanha não está recebendo doações porque foi concluída, cancelada ou teve o prazo encerrado. Escolha outra campanha ativa no painel.");

        Id = Guid.NewGuid();
        CampanhaId = campanha.Id;
        DoadorId = doadorId;
        Valor = valor;
        Status = StatusDoacao.Pendente;
        CriadaEm = agora;
    }
}
