using GestaoTempo.Api.Data;
using GestaoTempo.Api.Dtos;
using GestaoTempo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoTempo.Api.Controllers;

[ApiController]
[Route("api/categorias")]
public class CategoriasController : ControllerAutenticado
{
    private readonly AppDbContext _db;

    public CategoriasController(AppDbContext db, UsuarioAtual usuarioAtual)
        : base(usuarioAtual)
    {
        _db = db;
    }

    // Categorias do usuário, para preencher o campo "Categoria" da tela Nova tarefa.
    [HttpGet]
    public async Task<ActionResult<List<CategoriaResponse>>> Listar()
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        return await _db.Categorias
            .Where(c => c.UsuarioId == usuarioId)
            .OrderBy(c => c.Id)
            .Select(c => new CategoriaResponse(c.Id, c.Nome, c.Cor, c.Padrao))
            .ToListAsync();
    }
}
