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

        if (ConferirCampos(req) is { } erro)
            return BadRequest(erro);

        // A categoria precisa existir e ser do próprio usuário.
        var categoria = await _db.Categorias
            .FirstOrDefaultAsync(c => c.Id == req.CategoriaId && c.UsuarioId == usuarioId);
        if (categoria is null)
            return BadRequest(new ErroResponse("Categoria inválida."));

        var prazo = PrazoLocal(req.Prazo!.Value);

        Tarefa tarefa;
        try
        {
            tarefa = new Tarefa(
                req.Titulo ?? string.Empty,
                string.IsNullOrWhiteSpace(req.Descricao) ? null! : req.Descricao,
                prazo,
                req.EstimativaMin,
                req.Criticidade!.Value,
                req.Complexidade!.Value,
                req.Prioridade!.Value,
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

    // US05 - Editar tarefa (mesmos campos do cadastro; o status muda pela US07)
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TarefaResponse>> Editar(int id, CriarTarefaRequest req)
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        // As subtarefas vêm junto: a estimativa não pode ficar menor que a soma delas.
        var tarefa = await _db.Tarefas
            .Include(t => t.Subtarefas)
            .FirstOrDefaultAsync(t => t.Id == id && t.UsuarioId == usuarioId);
        if (tarefa is null)
            return NotFound(new ErroResponse("Tarefa não encontrada."));

        if (ConferirCampos(req) is { } erro)
            return BadRequest(erro);

        var categoriaExiste = await _db.Categorias
            .AnyAsync(c => c.Id == req.CategoriaId && c.UsuarioId == usuarioId);
        if (!categoriaExiste)
            return BadRequest(new ErroResponse("Categoria inválida."));

        try
        {
            tarefa.Editar(
                req.Titulo ?? string.Empty,
                string.IsNullOrWhiteSpace(req.Descricao) ? null! : req.Descricao,
                PrazoLocal(req.Prazo!.Value),
                req.EstimativaMin,
                req.Criticidade!.Value,
                req.Complexidade!.Value,
                req.Prioridade!.Value);
            tarefa.TrocarCategoria(req.CategoriaId);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new ErroResponse(ex.Message));
        }

        await _db.SaveChangesAsync();
        return await Consultar(usuarioId, id).FirstAsync();
    }

    // US05 - Excluir tarefa. A tela pede confirmação antes de chamar.
    // As subtarefas (e as subtarefas delas) são excluídas junto.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        var existe = await _db.Tarefas.AnyAsync(t => t.Id == id && t.UsuarioId == usuarioId);
        if (!existe)
            return NotFound(new ErroResponse("Tarefa não encontrada."));

        // Junta a tarefa e todos os níveis de subtarefas abaixo dela.
        var niveis = new List<List<int>> { new() { id } };
        while (true)
        {
            var nivelAtual = niveis[^1];
            var filhas = await _db.Tarefas
                .Where(t => t.UsuarioId == usuarioId && t.TarefaPaiId != null && nivelAtual.Contains(t.TarefaPaiId.Value))
                .Select(t => t.Id)
                .ToListAsync();
            if (filhas.Count == 0) break;
            niveis.Add(filhas);
        }

        // O banco não deixa apagar a tarefa-pai antes das filhas, então apaga do último nível para o primeiro.
        await using var transacao = await _db.Database.BeginTransactionAsync();
        for (var i = niveis.Count - 1; i >= 0; i--)
        {
            var ids = niveis[i];
            await _db.Tarefas.Where(t => t.UsuarioId == usuarioId && ids.Contains(t.Id)).ExecuteDeleteAsync();
        }
        await transacao.CommitAsync();

        return NoContent();
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

    // Campos que a classe Tarefa não confere sozinha (cadastro e edição).
    private static ErroResponse? ConferirCampos(CriarTarefaRequest req)
    {
        if (req.Titulo is not null && req.Titulo.Length > TamanhoMaximoTitulo)
            return new ErroResponse($"O título pode ter no máximo {TamanhoMaximoTitulo} caracteres.");
        if (req.Prazo is null)
            return new ErroResponse("O prazo é obrigatório.");
        if (req.Criticidade is null || !Enum.IsDefined(req.Criticidade.Value))
            return new ErroResponse("Informe a criticidade.");
        if (req.Complexidade is null || !Enum.IsDefined(req.Complexidade.Value))
            return new ErroResponse("Informe a complexidade.");
        if (req.Prioridade is null || !Enum.IsDefined(req.Prioridade.Value))
            return new ErroResponse("Informe a prioridade.");
        return null;
    }

    // A classe Tarefa compara o prazo com o horário local.
    private static DateTime PrazoLocal(DateTime prazo) =>
        prazo.Kind == DateTimeKind.Utc ? prazo.ToLocalTime() : prazo;

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
