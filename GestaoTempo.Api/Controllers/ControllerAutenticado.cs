using GestaoTempo.Api.Dtos;
using GestaoTempo.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestaoTempo.Api.Controllers;

// Base dos controllers que precisam saber quem está logado.
public abstract class ControllerAutenticado : ControllerBase
{
    protected ControllerAutenticado(UsuarioAtual usuarioAtual)
    {
        UsuarioAtual = usuarioAtual;
    }

    protected UsuarioAtual UsuarioAtual { get; }

    // 401 padrão para quando o token (ou o cabeçalho de teste) não identifica ninguém.
    protected ActionResult NaoIdentificado() =>
        Unauthorized(new ErroResponse("Usuário não identificado."));
}
