using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GestaoTempo.Api.Services;

// Gera o token (JWT) do login (US02).
// O id do usuário vai na claim NameIdentifier, que é de onde a classe UsuarioAtual lê.
public class GeradorDeToken
{
    private readonly IConfiguration _config;

    public GeradorDeToken(IConfiguration config)
    {
        _config = config;
    }

    public (string Token, DateTime ExpiraEm) Gerar(int usuarioId, string nome, string email)
    {
        var expiraEm = DateTime.UtcNow.AddHours(_config.GetValue("Jwt:ValidadeHoras", 8));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new Claim(ClaimTypes.Name, nome),
            new Claim(ClaimTypes.Email, email),
        };

        var credenciais = new SigningCredentials(ChaveDeAssinatura(_config), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Emissor"],
            audience: _config["Jwt:Emissor"],
            claims: claims,
            expires: expiraEm,
            signingCredentials: credenciais);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }

    // A chave fica em appsettings.Development.json (Jwt:Chave). Em produção, precisa ser configurada.
    public static SymmetricSecurityKey ChaveDeAssinatura(IConfiguration config)
    {
        var chave = config["Jwt:Chave"];
        if (string.IsNullOrWhiteSpace(chave) || Encoding.UTF8.GetByteCount(chave) < 32)
            throw new InvalidOperationException("Configure Jwt:Chave com pelo menos 32 caracteres.");

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave));
    }
}
