using Campanhas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Campanhas.Infrastructure.Data.Mappings;

public class RefreshTokenMapping : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(t => t.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(t => t.Familia).HasColumnName("familia").IsRequired();
        // SHA-256 em hexadecimal: 64 caracteres.
        builder.Property(t => t.TokenHash).HasColumnName("token_hash").IsRequired().HasMaxLength(64).IsFixedLength();
        builder.Property(t => t.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(t => t.ExpiraEm).HasColumnName("expira_em").IsRequired();
        builder.Property(t => t.SubstituidoEm).HasColumnName("substituido_em");
        builder.Property(t => t.RevogadoEm).HasColumnName("revogado_em");

        builder.HasOne<Usuario>().WithMany().HasForeignKey(t => t.UsuarioId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TokenHash).IsUnique().HasDatabaseName("ix_refresh_tokens_hash");
        builder.HasIndex(t => t.Familia).HasDatabaseName("ix_refresh_tokens_familia");
        builder.HasIndex(t => t.UsuarioId).HasDatabaseName("ix_refresh_tokens_usuario");
    }
}
