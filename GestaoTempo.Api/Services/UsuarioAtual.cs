using System.Security.Claims;

namespace GestaoTempo.Api.Services;

// Descobre qual usuário está fazendo a requisição.
//
// O token do login (US02) traz o id do usuário na claim NameIdentifier.
//
// Só em ambiente de desenvolvimento, também dá para informar o usuário pelo
// cabeçalho "X-Usuario-Id" (ex.: X-Usuario-Id: 1), para os testes do .http.
public class UsuarioAtual
{
    private readonly IHttpContextAccessor _http;
    private readonly IWebHostEnvironment _env;

    public UsuarioAtual(IHttpContextAccessor http, IWebHostEnvironment env)
    {
        _http = http;
        _env = env;
    }

    public int? Id
    {
        get
        {
            var contexto = _http.HttpContext;
            if (contexto is null) return null;

            var claim = contexto.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claim, out var idDoToken)) return idDoToken;

            if (_env.IsDevelopment()
                && contexto.Request.Headers.TryGetValue("X-Usuario-Id", out var valor)
                && int.TryParse(valor, out var idDoCabecalho))
                return idDoCabecalho;

            return null;
        }
    }
}
