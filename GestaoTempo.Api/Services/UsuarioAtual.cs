using System.Security.Claims;

namespace GestaoTempo.Api.Services;

// Descobre qual usuário está fazendo a requisição.
//
// Quando o login (US02) estiver pronto, o token deve trazer o id do usuário
// na claim NameIdentifier; esta classe já lê dali.
//
// Enquanto o login não existe, em ambiente de desenvolvimento dá para
// informar o usuário pelo cabeçalho "X-Usuario-Id" (ex.: X-Usuario-Id: 1).
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
