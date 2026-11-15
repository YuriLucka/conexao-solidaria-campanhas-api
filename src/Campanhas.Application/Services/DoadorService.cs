using Campanhas.Application.DTOs;
using Campanhas.Application.Exceptions;
using Campanhas.Application.Interfaces;
using Campanhas.Domain.Entities;
using Campanhas.Domain.Enums;
using Campanhas.Domain.Interfaces;
using Campanhas.Domain.ValueObjects;

namespace Campanhas.Application.Services;

public class DoadorService : IDoadorService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _uow;
    private readonly ISenhaHasher _hasher;
    private readonly TimeProvider _relogio;

    public DoadorService(IUsuarioRepository usuarios, IUnitOfWork uow, ISenhaHasher hasher, TimeProvider relogio)
    {
        _usuarios = usuarios;
        _uow = uow;
        _hasher = hasher;
        _relogio = relogio;
    }

    // Cadastro público cria sempre o perfil Doador; GestorONG só existe pelo seed.
    public async Task<DoadorResponse> CadastrarAsync(CadastrarDoadorRequest request, CancellationToken ct = default)
    {
        var email = new Email(request.Email);
        var cpf = new Cpf(request.Cpf);
        Usuario.ValidarSenha(request.Senha);

        if (await _usuarios.EmailExisteAsync(email.Value, ct))
            throw new ConflictException("Já existe uma conta com este e-mail. Entre com ela ou use outro e-mail.");
        if (await _usuarios.CpfExisteAsync(cpf.Value, ct))
            throw new ConflictException("Já existe uma conta com este CPF. Entre com ela ou fale com a ONG.");

        var doador = new Usuario(
            request.NomeCompleto, email.Value, cpf.Value, _hasher.Hash(request.Senha),
            Perfil.Doador, _relogio.GetUtcNow().UtcDateTime);

        await _usuarios.AdicionarAsync(doador, ct);
        await _uow.SalvarAsync(ct);

        return new DoadorResponse(doador.Id, doador.Nome, doador.Email.Value);
    }
}
