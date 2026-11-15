using Campanhas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Campanhas.Infrastructure.Data.Mappings;

// Nomes de tabela e coluna explícitos: o doacoes-worker usa SQL direto nestas tabelas.
public class CampanhaMapping : IEntityTypeConfiguration<Campanha>
{
    public void Configure(EntityTypeBuilder<Campanha> builder)
    {
        builder.ToTable("campanhas", t => t.HasCheckConstraint("ck_campanhas_meta_positiva", "meta_financeira > 0"));
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(c => c.Titulo).HasColumnName("titulo").IsRequired().HasMaxLength(150);
        builder.Property(c => c.Descricao).HasColumnName("descricao").IsRequired().HasMaxLength(4000);
        builder.Property(c => c.DataInicio).HasColumnName("data_inicio").IsRequired();
        builder.Property(c => c.DataFim).HasColumnName("data_fim").IsRequired();
        builder.Property(c => c.MetaFinanceira).HasColumnName("meta_financeira").HasPrecision(14, 2).IsRequired();
        builder.Property(c => c.Status).HasColumnName("status").IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(c => c.Status).HasDatabaseName("ix_campanhas_status");

        // Requisito do enunciado: a API não atualiza o valor arrecadado. O EF nunca grava esta coluna
        // (nem no INSERT, que usa o default 0 do banco, nem no UPDATE); quem soma é o doacoes-worker.
        var valor = builder.Property(c => c.ValorArrecadado).HasColumnName("valor_arrecadado")
            .HasPrecision(14, 2).IsRequired().HasDefaultValue(0m);
        valor.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
        valor.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        builder.Property(c => c.CriadaEm).HasColumnName("criada_em").IsRequired();
        builder.Property(c => c.AtualizadaEm).HasColumnName("atualizada_em").IsRequired();
    }
}
