using Campanhas.Application.Interfaces;
using Campanhas.Domain.Entities;
using Campanhas.Domain.Enums;
using Campanhas.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Campanhas.Infrastructure.Data;

// Dados de demonstração (Seed__Enabled=true). Datas relativas ao momento da subida, para as campanhas
// não vencerem antes da correção. O valor arrecadado começa em zero: só cresce pelo fluxo real (API -> RabbitMQ -> worker).
public static class DbSeeder
{
    public const string EmailGestora = "helena@esperancasolidaria.demo";

    public static async Task SeedAsync(AppDbContext db, ISenhaHasher hasher, DateTime agora, CancellationToken ct = default)
    {
        // A gestora pode já existir (criada como gestora inicial); os dados de demonstração entram uma vez só.
        if (await db.Usuarios.AnyAsync(u => u.Email == new Email("mariana@email.demo"), ct))
            return;

        if (!await db.Usuarios.AnyAsync(u => u.Email == new Email(EmailGestora), ct))
            db.Usuarios.Add(new Usuario("Helena Martins", EmailGestora, "111.444.777-35", hasher.Hash("Gestao@2026"), Perfil.GestorONG, agora));
        db.Usuarios.Add(new Usuario("Mariana Albuquerque", "mariana@email.demo", "529.982.247-25", hasher.Hash("Doar@2026"), Perfil.Doador, agora));

        var hoje = agora.Date;
        Campanha Nova(string titulo, string descricao, int inicioEmDias, int fimEmDias, decimal meta, string categoria, string capa, int criancas) =>
            new(titulo, descricao, hoje.AddDays(inicioEmDias).AddHours(9), hoje.AddDays(fimEmDias).AddHours(23).AddMinutes(59), meta, agora,
                categoria, capa, criancas);

        var campanhas = new[]
        {
            Nova("Natal da Esperança", "Presentes, roupas novas e uma ceia de Natal para as 220 crianças acolhidas pela ONG. Cada criança escreve sua cartinha, e a ONG se empenha para atender aos pedidos.", -55, 40, 60000m, "Datas especiais", "presentes", 220),
            Nova("Volta às Aulas", "Kits escolares completos para 180 crianças: mochila, cadernos, estojo, uniforme e tênis, montados por voluntários e entregues antes do início das aulas.", -20, 75, 45000m, "Educação", "livros", 180),
            Nova("Reforma do Refeitório", "Troca do piso, nova cozinha industrial e mesas para 120 lugares. O refeitório atual tem mais de 20 anos e serve cerca de 600 refeições por dia.", -90, 120, 85000m, "Infraestrutura", "casa", 220),
            Nova("Biblioteca Viva", "Compra de 500 livros infantis e juvenis, estantes baixas e um cantinho de leitura, com oficinas semanais de contação de histórias.", -10, 60, 18000m, "Educação", "estrelas", 220),
            Nova("Saúde em Dia", "Tratamento odontológico e consultas oftalmológicas para 95 crianças, incluindo óculos quando houver indicação.", -30, 50, 32000m, "Saúde", "saude", 95),
            Nova("Horta que Ensina", "Uma horta comunitária no terreno da ONG, com oficinas de plantio e alimentação saudável. Parte da colheita vai para o refeitório.", -5, 90, 12000m, "Alimentação", "horta", 120)
        };
        db.Campanhas.AddRange(campanhas);

        // Encerradas antes do prazo: não aparecem no painel (só Ativa aparece).
        var inverno = Nova("Inverno Quentinho", "Cobertores, agasalhos e meias para que as crianças acolhidas atravessem o inverno aquecidas.", -150, 10, 25000m, "Inverno", "inverno", 220);
        inverno.Atualizar(inverno.Titulo, inverno.Descricao, inverno.DataInicio, inverno.DataFim, inverno.MetaFinanceira, StatusCampanha.Concluida, agora,
            inverno.Categoria, inverno.Capa, inverno.CriancasAtendidas);
        var passeio = Nova("Passeio ao Parque Ecológico", "Passeio de um dia com transporte e lanche. Campanha cancelada porque não foi possível garantir transporte adaptado para todas as crianças.", -60, 15, 6000m, "Esporte e lazer", "sol-colinas", 60);
        passeio.Atualizar(passeio.Titulo, passeio.Descricao, passeio.DataInicio, passeio.DataFim, passeio.MetaFinanceira, StatusCampanha.Cancelada, agora,
            passeio.Categoria, passeio.Capa, passeio.CriancasAtendidas);
        db.Campanhas.AddRange(inverno, passeio);

        await db.SaveChangesAsync(ct);
    }
}
