using GestaoTempo.Api.Data;
using GestaoTempo.Api.Dtos;
using GestaoTempo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoTempo.Api.Controllers;

[ApiController]
[Route("api")]
public class AutenticacaoController : ControllerBase
{
    // Mesma mensagem para e-mail que não existe e para senha errada,
    // para não revelar quais e-mails têm conta (US02).
    private const string MensagemLoginInvalido = "E-mail ou senha inválidos.";

    // Hash usado quando o e-mail não existe, para a resposta demorar o mesmo tempo.
    private static readonly string HashFalso = BCrypt.Net.BCrypt.HashPassword("senha-inexistente");

    private readonly AppDbContext _db;
    private readonly GeradorDeToken _geradorDeToken;

    public AutenticacaoController(AppDbContext db, GeradorDeToken geradorDeToken)
    {
        _db = db;
        _geradorDeToken = geradorDeToken;
    }

    // US02 - Entrar
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Entrar(LoginRequest req)
    {
        var email = req.Email?.Trim() ?? string.Empty;
        var senha = req.Senha ?? string.Empty;

        if (email.Length == 0 || senha.Length == 0)
            return BadRequest(new ErroResponse("Informe e-mail e senha."));

        var usuario = await _db.Usuarios
            .Where(u => u.Email == email)
            .Select(u => new { u.Id, u.Nome, u.Email, u.SenhaHash })
            .FirstOrDefaultAsync();

        if (!SenhaConfere(senha, usuario?.SenhaHash ?? HashFalso) || usuario is null)
            return Unauthorized(new ErroResponse(MensagemLoginInvalido));

        var (token, expiraEm) = _geradorDeToken.Gerar(usuario.Id, usuario.Nome, usuario.Email);
        return new LoginResponse(token, expiraEm, new UsuarioResponse(usuario.Id, usuario.Nome, usuario.Email));
    }

    // US02 - Sair: o token não fica guardado no servidor, então sair é o front descartar o token.

    private static bool SenhaConfere(string senha, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(senha, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash gravado em formato que não é BCrypt.
            return false;
        }
    }
}
