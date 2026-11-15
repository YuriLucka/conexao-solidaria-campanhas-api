using Campanhas.Domain.Entities;
using Campanhas.Domain.Enums;
using Campanhas.Infrastructure.Data;
using Campanhas.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Campanhas.Tests.Infrastructure;

public class InfrastructureTests
{
    // Requisito do enunciado: a API não pode atualizar o valor arrecadado direto no banco.
    // Só olha o modelo do EF (não precisa de PostgreSQL rodando).
    [Fact]
    public void EF_nunca_grava_valor_arrecadado()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=nao-usado").Options;
        using var db = new AppDbContext(options);

        var propriedade = db.Model.FindEntityType(typeof(Campanha))!.FindProperty(nameof(Campanha.ValorArrecadado))!;

        Assert.Equal(PropertySaveBehavior.Ignore, propriedade.GetBeforeSaveBehavior());
        Assert.Equal(PropertySaveBehavior.Ignore, propriedade.GetAfterSaveBehavior());
        Assert.Equal("valor_arrecadado", propriedade.GetColumnName());
    }

    [Fact]
    public void Token_tem_sub_role_e_expiracao()
    {
        var agora = new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
        var settings = Options.Create(new JwtSettings { SecretKey = new string('k', 40), ExpirationMinutes = 120 });
        var usuario = new Usuario("Helena", "helena@ong.demo", "111.444.777-35", "hash", Perfil.GestorONG, agora.UtcDateTime);

        var resposta = new JwtTokenService(settings, new FakeTimeProvider(agora)).Gerar(usuario);
        var jwt = new JsonWebToken(resposta.AccessToken);

        Assert.Equal(usuario.Id.ToString(), jwt.Subject);
        Assert.Equal("GestorONG", jwt.GetClaim("role").Value);
        Assert.Equal("conexao-solidaria", jwt.Issuer);
        Assert.Equal(agora.UtcDateTime.AddMinutes(120), resposta.ExpiraEm);
        Assert.Equal("GestorONG", resposta.Perfil);
    }
}
