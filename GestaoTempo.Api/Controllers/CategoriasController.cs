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
    private const int TamanhoMaximoNome = 50;
    private const string CorPadrao = "#9AA6B2";

    private readonly AppDbContext _db;

    public CategoriasController(AppDbContext db, UsuarioAtual usuarioAtual)
        : base(usuarioAtual)
    {
        _db = db;
    }

    // Categorias do usuário, com quantas tarefas cada uma tem (telas Nova tarefa e Categorias).
    [HttpGet]
    public async Task<ActionResult<List<CategoriaResponse>>> Listar()
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        return await Consultar(usuarioId).ToListAsync();
    }

    // US04 - Criar categoria própria
    [HttpPost]
    public async Task<ActionResult<CategoriaResponse>> Criar(SalvarCategoriaRequest req)
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        var nome = req.Nome?.Trim() ?? string.Empty;
        var cor = string.IsNullOrWhiteSpace(req.Cor) ? CorPadrao : req.Cor.Trim();

        if (ConferirCampos(nome, cor) is { } erro)
            return BadRequest(erro);
        if (await NomeEmUso(usuarioId, nome, idIgnorado: null))
            return Conflict(new ErroResponse("Você já tem uma categoria com esse nome."));

        Categoria categoria;
        try
        {
            categoria = new Categoria(nome, cor, usuarioId);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErroResponse(ex.Message));
        }

        _db.Categorias.Add(categoria);
        await _db.SaveChangesAsync();

        var resposta = await Consultar(usuarioId, categoria.Id).FirstAsync();
        return CreatedAtAction(nameof(Listar), resposta);
    }

    // US04 - Renomear (e trocar a cor de) uma categoria própria
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoriaResponse>> Editar(int id, SalvarCategoriaRequest req)
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        if (categoria is null)
            return NotFound(new ErroResponse("Categoria não encontrada."));
        if (categoria.Padrao)
            return BadRequest(new ErroResponse("As categorias padrão não podem ser alteradas."));

        var nome = req.Nome?.Trim() ?? string.Empty;
        var cor = string.IsNullOrWhiteSpace(req.Cor) ? categoria.Cor : req.Cor.Trim();

        if (ConferirCampos(nome, cor) is { } erro)
            return BadRequest(erro);
        if (await NomeEmUso(usuarioId, nome, idIgnorado: id))
            return Conflict(new ErroResponse("Você já tem uma categoria com esse nome."));

        try
        {
            categoria.Renomear(nome);
            categoria.AlterarCor(cor);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErroResponse(ex.Message));
        }

        await _db.SaveChangesAsync();
        return await Consultar(usuarioId, id).FirstAsync();
    }

    // US04 - Excluir categoria própria.
    // Se tiver tarefas, é preciso informar para qual categoria elas vão (?moverPara=id).
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id, [FromQuery] int? moverPara)
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        if (categoria is null)
            return NotFound(new ErroResponse("Categoria não encontrada."));
        if (categoria.Padrao)
            return BadRequest(new ErroResponse("As categorias padrão não podem ser excluídas."));

        var tarefas = await _db.Tarefas
            .Where(t => t.CategoriaId == id && t.UsuarioId == usuarioId)
            .ToListAsync();

        if (tarefas.Count > 0)
        {
            if (moverPara is null)
                return Conflict(new ErroResponse(
                    $"Esta categoria tem {tarefas.Count} tarefa(s). Escolha para qual categoria elas vão antes de excluir."));

            var destinoExiste = await _db.Categorias
                .AnyAsync(c => c.Id == moverPara && c.UsuarioId == usuarioId && c.Id != id);
            if (!destinoExiste)
                return BadRequest(new ErroResponse("Categoria de destino inválida."));

            foreach (var tarefa in tarefas)
                tarefa.TrocarCategoria(moverPara.Value);
        }

        // Mover as tarefas e excluir a categoria são gravados juntos.
        _db.Categorias.Remove(categoria);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ErroResponse? ConferirCampos(string nome, string? cor)
    {
        if (nome.Length < 3)
            return new ErroResponse("O nome da categoria deve ter pelo menos 3 caracteres.");
        if (nome.Length > TamanhoMaximoNome)
            return new ErroResponse($"O nome da categoria pode ter no máximo {TamanhoMaximoNome} caracteres.");
        if (!Categoria.CorValida(cor))
            return new ErroResponse("A cor deve estar no formato #RRGGBB.");
        return null;
    }

    // Nome repetido não vale, nem com maiúsculas diferentes ("Academia" e "academia").
    private Task<bool> NomeEmUso(int usuarioId, string nome, int? idIgnorado)
    {
        var nomeMinusculo = nome.ToLower();
        return _db.Categorias.AnyAsync(c =>
            c.UsuarioId == usuarioId && c.Nome.ToLower() == nomeMinusculo && c.Id != idIgnorado);
    }

    private IQueryable<CategoriaResponse> Consultar(int usuarioId, int? categoriaId = null) =>
        _db.Categorias
            .Where(c => c.UsuarioId == usuarioId && (categoriaId == null || c.Id == categoriaId))
            .OrderBy(c => c.Id)
            .Select(c => new CategoriaResponse(
                c.Id, c.Nome, c.Cor, c.Padrao,
                _db.Tarefas.Count(t => t.CategoriaId == c.Id)));
}
