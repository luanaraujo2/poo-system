using GestaoTempo.Api.Data;
using GestaoTempo.Api.Dtos;
using GestaoTempo.Api.Services;
using GestaoTempo.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GestaoTempo.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    public const int TamanhoMinimoSenha = 8;

    private readonly AppDbContext _db;
    private readonly UsuarioAtual _usuarioAtual;

    public UsuariosController(AppDbContext db, UsuarioAtual usuarioAtual)
    {
        _db = db;
        _usuarioAtual = usuarioAtual;
    }

    // US01 - Cadastrar conta
    [HttpPost]
    public async Task<ActionResult<UsuarioResponse>> Cadastrar(CadastrarUsuarioRequest req)
    {
        var nome = req.Nome?.Trim() ?? string.Empty;
        var email = req.Email?.Trim() ?? string.Empty;
        var senha = req.Senha ?? string.Empty;

        if (senha.Length < TamanhoMinimoSenha)
            return BadRequest(new ErroResponse($"A senha deve ter pelo menos {TamanhoMinimoSenha} caracteres."));

        // A coluna email é citext: a comparação já ignora maiúsculas e minúsculas.
        if (await _db.Usuarios.AnyAsync(u => u.Email == email))
            return Conflict(new ErroResponse("Este e-mail já está cadastrado."));

        Usuario usuario;
        try
        {
            // A senha nunca é gravada: só o hash (BCrypt).
            // O construtor de Usuario valida nome e e-mail e já cria as 5 categorias padrão.
            usuario = new Usuario(nome, email, BCrypt.Net.BCrypt.HashPassword(senha));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErroResponse(ex.Message));
        }

        _db.Usuarios.Add(usuario);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Dois cadastros com o mesmo e-mail ao mesmo tempo: o índice único do banco segura.
            return Conflict(new ErroResponse("Este e-mail já está cadastrado."));
        }

        var resposta = new UsuarioResponse(usuario.Id, usuario.Nome, usuario.Email);
        return CreatedAtAction(nameof(Eu), null, resposta);
    }

    // Dados do usuário logado (usado pelo front para mostrar o nome).
    [HttpGet("eu")]
    public async Task<ActionResult<UsuarioResponse>> Eu()
    {
        if (_usuarioAtual.Id is not int usuarioId)
            return Unauthorized(new ErroResponse("Usuário não identificado."));

        // Leitura por projeção: não monta a entidade Usuario (o construtor dela cria categorias).
        var usuario = await _db.Usuarios
            .Where(u => u.Id == usuarioId)
            .Select(u => new UsuarioResponse(u.Id, u.Nome, u.Email))
            .FirstOrDefaultAsync();

        return usuario is null ? Unauthorized(new ErroResponse("Usuário não identificado.")) : usuario;
    }
}
