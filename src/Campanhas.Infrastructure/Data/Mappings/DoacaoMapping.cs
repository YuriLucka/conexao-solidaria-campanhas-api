using Campanhas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Campanhas.Infrastructure.Data.Mappings;

// Nomes de tabela e coluna explícitos: o doacoes-worker usa SQL direto nestas tabelas.
public class DoacaoMapping : IEntityTypeConfiguration<Doacao>
{
    public void Configure(EntityTypeBuilder<Doacao> builder)
    {
        builder.ToTable("doacoes", t => t.HasCheckConstraint("ck_doacoes_valor_positivo", "valor > 0"));
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(d => d.CampanhaId).HasColumnName("campanha_id").IsRequired();
        builder.Property(d => d.DoadorId).HasColumnName("doador_id").IsRequired();
        builder.Property(d => d.Valor).HasColumnName("valor").HasPrecision(14, 2).IsRequired();
        builder.Property(d => d.Status).HasColumnName("status").IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.MotivoRejeicao).HasColumnName("motivo_rejeicao").HasMaxLength(300);
        builder.Property(d => d.CriadaEm).HasColumnName("criada_em").IsRequired();
        builder.Property(d => d.ProcessadaEm).HasColumnName("processada_em");

        builder.HasOne<Campanha>().WithMany().HasForeignKey(d => d.CampanhaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(d => d.DoadorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.CampanhaId).HasDatabaseName("ix_doacoes_campanha_id");
        builder.HasIndex(d => d.DoadorId).HasDatabaseName("ix_doacoes_doador_id");
    }
}
