using Campanhas.Domain.Entities;
using Campanhas.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Campanhas.Infrastructure.Data.Mappings;

public class UsuarioMapping : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(u => u.Nome).HasColumnName("nome").IsRequired().HasMaxLength(150);

        builder.Property(u => u.Email).HasColumnName("email").IsRequired().HasMaxLength(200)
            .HasConversion(email => email.Value, value => new Email(value));
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ix_usuarios_email");

        builder.Property(u => u.Cpf).HasColumnName("cpf").IsRequired().HasMaxLength(11).IsFixedLength()
            .HasConversion(cpf => cpf.Value, value => new Cpf(value));
        builder.HasIndex(u => u.Cpf).IsUnique().HasDatabaseName("ix_usuarios_cpf");

        builder.Property(u => u.SenhaHash).HasColumnName("senha_hash").IsRequired();
        builder.Property(u => u.Perfil).HasColumnName("perfil").IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.CriadoEm).HasColumnName("criado_em").IsRequired();
    }
}
