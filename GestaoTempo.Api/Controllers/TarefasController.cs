using GestaoTempo.Api.Data;
using GestaoTempo.Api.Dtos;
using GestaoTempo.Api.Services;
using GestaoTempo.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoTempo.Api.Controllers;

[ApiController]
[Route("api/tarefas")]
public class TarefasController : ControllerAutenticado
{
    private const int TamanhoMaximoTitulo = 150;

    private readonly AppDbContext _db;

    public TarefasController(AppDbContext db, UsuarioAtual usuarioAtual)
        : base(usuarioAtual)
    {
        _db = db;
    }

    // US03 - Cadastrar tarefa
    [HttpPost]
    public async Task<ActionResult<TarefaResponse>> Criar(CriarTarefaRequest req)
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        // Campos que a classe Tarefa não confere sozinha.
        if (req.Titulo is not null && req.Titulo.Length > TamanhoMaximoTitulo)
            return BadRequest(new ErroResponse($"O título pode ter no máximo {TamanhoMaximoTitulo} caracteres."));
        if (req.Prazo is null)
            return BadRequest(new ErroResponse("O prazo é obrigatório."));
        if (req.Criticidade is null || !Enum.IsDefined(req.Criticidade.Value))
            return BadRequest(new ErroResponse("Informe a criticidade."));
        if (req.Complexidade is null || !Enum.IsDefined(req.Complexidade.Value))
            return BadRequest(new ErroResponse("Informe a complexidade."));
        if (req.Prioridade is null || !Enum.IsDefined(req.Prioridade.Value))
            return BadRequest(new ErroResponse("Informe a prioridade."));

        // A categoria precisa existir e ser do próprio usuário.
        var categoria = await _db.Categorias
            .FirstOrDefaultAsync(c => c.Id == req.CategoriaId && c.UsuarioId == usuarioId);
        if (categoria is null)
            return BadRequest(new ErroResponse("Categoria inválida."));

        // A classe Tarefa compara o prazo com o horário local.
        var prazo = req.Prazo.Value.Kind == DateTimeKind.Utc ? req.Prazo.Value.ToLocalTime() : req.Prazo.Value;

        Tarefa tarefa;
        try
        {
            tarefa = new Tarefa(
                req.Titulo ?? string.Empty,
                string.IsNullOrWhiteSpace(req.Descricao) ? null! : req.Descricao,
                prazo,
                req.EstimativaMin,
                req.Criticidade.Value,
                req.Complexidade.Value,
                req.Prioridade.Value,
                usuarioId,
                categoria.Id);
        }
        catch (ArgumentException ex)
        {
            // Regras da classe Tarefa: título, tempo estimado e prazo.
            return BadRequest(new ErroResponse(ex.Message));
        }

        _db.Tarefas.Add(tarefa);
        await _db.SaveChangesAsync();

        var resposta = await Consultar(usuarioId, tarefa.Id).FirstAsync();
        return CreatedAtAction(nameof(Obter), new { id = tarefa.Id }, resposta);
    }

    // Vincula duas tarefas já cadastradas, mantendo o vínculo dentro do usuário autenticado.
    [HttpPost("{idPai:int}/subtarefas/{idFilha:int}")]
    public async Task<IActionResult> AdicionarSubtarefa(int idPai, int idFilha)
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        var pai = await _db.Tarefas.FirstOrDefaultAsync(t => t.Id == idPai && t.UsuarioId == usuarioId);
        var filha = await _db.Tarefas.FirstOrDefaultAsync(t => t.Id == idFilha && t.UsuarioId == usuarioId);
        if (pai is null || filha is null)
            return NotFound(new ErroResponse("Tarefa não encontrada."));

        try
        {
            pai.AdicionarSubtarefa(filha);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErroResponse(ex.Message));
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TarefaResponse>> Obter(int id)
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        var tarefa = await Consultar(usuarioId, id).FirstOrDefaultAsync();
        if (tarefa is null) return NotFound();

        return tarefa;
    }

    // Lista as tarefas do usuário, para conferir o que foi cadastrado.
    [HttpGet]
    public async Task<ActionResult<List<TarefaResponse>>> Listar()
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        return await Consultar(usuarioId).ToListAsync();
    }

    // Tarefas do usuário (ou só uma, se informar o id), já com o nome da categoria, por ordem de prazo.
    private IQueryable<TarefaResponse> Consultar(int usuarioId, int? tarefaId = null) =>
        from t in _db.Tarefas
        join c in _db.Categorias on t.CategoriaId equals c.Id
        where t.UsuarioId == usuarioId && (tarefaId == null || t.Id == tarefaId)
        orderby t.Prazo
        select new TarefaResponse(
            t.Id, t.Titulo, t.Descricao, t.Prazo, t.EstimativaMin,
            c.Id, c.Nome, t.Criticidade, t.Complexidade, t.Prioridade, t.Status, t.CriadoEm,
            t.Subtarefas.Select(sub => sub.Id).ToList());
}
