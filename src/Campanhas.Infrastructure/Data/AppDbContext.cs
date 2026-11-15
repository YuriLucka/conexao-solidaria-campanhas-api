using Campanhas.Application.Exceptions;
using Campanhas.Domain.Entities;
using Campanhas.Domain.Interfaces;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Campanhas.Infrastructure.Data;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Campanha> Campanhas => Set<Campanha>();
    public DbSet<Doacao> Doacoes => Set<Doacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tabelas do outbox transacional do MassTransit (evento gravado junto com a doação).
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public async Task SalvarAsync(CancellationToken ct = default)
    {
        try
        {
            await SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            // Corrida entre duas requisições: a checagem prévia passou, o índice único barrou.
            throw new ConflictException(pg.ConstraintName switch
            {
                "ix_usuarios_email" => "Já existe uma conta com este e-mail.",
                "ix_usuarios_cpf" => "Já existe uma conta com este CPF.",
                _ => "Registro duplicado."
            });
        }
    }
}
