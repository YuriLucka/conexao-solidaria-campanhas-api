using Campanhas.Domain.Enums;
using Campanhas.Domain.Exceptions;

namespace Campanhas.Domain.Entities;

public class Campanha
{
    public Guid Id { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string Descricao { get; private set; } = string.Empty;
    public DateTime DataInicio { get; private set; }
    public DateTime DataFim { get; private set; }
    public decimal MetaFinanceira { get; private set; }
    public StatusCampanha Status { get; private set; }

    // Opcionais, para o site (card e página da campanha). Valores iguais aos do protótipo do grupo.
    public string? Categoria { get; private set; }
    public string? Capa { get; private set; }
    public int? CriancasAtendidas { get; private set; }

    public static readonly IReadOnlyList<string> Categorias =
        ["Datas especiais", "Educação", "Infraestrutura", "Saúde", "Alimentação", "Esporte e lazer", "Inverno"];

    public static readonly IReadOnlyList<string> Capas =
        ["sol-colinas", "amanhecer", "livros", "presentes", "casa", "saude", "horta", "bola", "inverno", "estrelas", "abraco", "festa"];

    // Só o doacoes-worker altera este valor (direto no banco). Aqui não existe método que o modifique,
    // e o mapeamento do EF ignora a coluna ao salvar.
    public decimal ValorArrecadado { get; private set; }

    public DateTime CriadaEm { get; private set; }
    public DateTime AtualizadaEm { get; private set; }

    // EF Core
    protected Campanha() { }

    // Toda campanha nasce Ativa.
    public Campanha(string titulo, string descricao, DateTime dataInicio, DateTime dataFim, decimal metaFinanceira, DateTime agora,
        string? categoria = null, string? capa = null, int? criancasAtendidas = null)
    {
        ValidarCampos(titulo, descricao, dataInicio, dataFim, metaFinanceira);
        ValidarTerminoNoFuturo(dataFim, agora);
        DefinirDetalhes(categoria, capa, criancasAtendidas);

        Id = Guid.NewGuid();
        Titulo = titulo.Trim();
        Descricao = descricao.Trim();
        DataInicio = dataInicio;
        DataFim = dataFim;
        MetaFinanceira = metaFinanceira;
        Status = StatusCampanha.Ativa;
        CriadaEm = agora;
        AtualizadaEm = agora;
    }

    public void Atualizar(string titulo, string descricao, DateTime dataInicio, DateTime dataFim, decimal metaFinanceira, StatusCampanha status, DateTime agora,
        string? categoria = null, string? capa = null, int? criancasAtendidas = null)
    {
        ValidarCampos(titulo, descricao, dataInicio, dataFim, metaFinanceira);
        if (!Enum.IsDefined(status))
            throw new DomainException("Status inválido. Use um destes valores: Ativa, Concluida ou Cancelada.");

        // Mesma regra da criação quando o término muda ou a campanha volta a ficar Ativa.
        var reabrindo = status == StatusCampanha.Ativa && Status != StatusCampanha.Ativa;
        if (dataFim != DataFim || reabrindo)
            ValidarTerminoNoFuturo(dataFim, agora);

        DefinirDetalhes(categoria, capa, criancasAtendidas);

        Titulo = titulo.Trim();
        Descricao = descricao.Trim();
        DataInicio = dataInicio;
        DataFim = dataFim;
        MetaFinanceira = metaFinanceira;
        Status = status;
        AtualizadaEm = agora;
    }

    public bool PrazoEncerrado(DateTime agora) => DataFim < agora;

    // "Encerrada" no enunciado: Concluida, ou Ativa com o prazo vencido.
    public bool AceitaDoacao(DateTime agora) => Status == StatusCampanha.Ativa && !PrazoEncerrado(agora);

    private static void ValidarCampos(string titulo, string descricao, DateTime dataInicio, DateTime dataFim, decimal metaFinanceira)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new DomainException("O título é obrigatório.");
        if (titulo.Trim().Length > 150)
            throw new DomainException("O título deve ter no máximo 150 caracteres.");
        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("A descrição é obrigatória.");
        if (descricao.Trim().Length > 4000)
            throw new DomainException("A descrição deve ter no máximo 4.000 caracteres.");
        if (dataInicio > dataFim)
            throw new DomainException("A data de início não pode ser posterior à data de término.");
        if (metaFinanceira <= 0)
            throw new DomainException("A meta financeira deve ser maior que zero.");
        if (metaFinanceira > 999_999_999_999.99m)
            throw new DomainException("A meta financeira excede o limite permitido (R$ 999.999.999.999,99).");
        if (decimal.Round(metaFinanceira, 2) != metaFinanceira)
            throw new DomainException("A meta financeira deve ter no máximo duas casas decimais.");
    }

    private void DefinirDetalhes(string? categoria, string? capa, int? criancasAtendidas)
    {
        categoria = string.IsNullOrWhiteSpace(categoria) ? null : categoria.Trim();
        capa = string.IsNullOrWhiteSpace(capa) ? null : capa.Trim();

        if (categoria is not null && !Categorias.Contains(categoria))
            throw new DomainException($"Categoria inválida. Use uma destas: {string.Join(", ", Categorias)}.");
        if (capa is not null && !Capas.Contains(capa))
            throw new DomainException($"Capa inválida. Use uma destas: {string.Join(", ", Capas)}.");
        if (criancasAtendidas is < 0 or > 100_000)
            throw new DomainException("O número de crianças atendidas deve estar entre 0 e 100.000.");

        Categoria = categoria;
        Capa = capa;
        CriancasAtendidas = criancasAtendidas;
    }

    private static void ValidarTerminoNoFuturo(DateTime dataFim, DateTime agora)
    {
        if (dataFim < agora)
            throw new DomainException("A data de término não pode estar no passado.");
    }
}
