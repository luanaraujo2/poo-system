using GestaoTempo.Api.Data;
using GestaoTempo.Api.Dtos;
using GestaoTempo.Api.Services;
using GestaoTempo.Domain.Entities;
using GestaoTempo.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace GestaoTempo.Api.Controllers;

[ApiController]
[Route("api/planejamento")]
public class PlanejamentoController : ControllerAutenticado
{
    private readonly AppDbContext _db;

    public PlanejamentoController(AppDbContext db, UsuarioAtual usuarioAtual) : base(usuarioAtual)
    {
        _db = db;
    }

    // US11 - Consulta o planejamento salvo da semana (ou a semana atual, se nenhuma data for informada).
    [HttpGet]
    public async Task<ActionResult<PlanejamentoSemanaResponse>> Obter([FromQuery] DateOnly? semanaInicio = null)
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        var semana = PlanejadorSemanal.InicioDaSemana(semanaInicio ?? DateOnly.FromDateTime(DateTime.Now));
        return await MontarResposta(usuarioId, semana);
    }

    // US11 - Distribui as tarefas principais abertas nos blocos de tempo livre da semana, respeitando prazos.
    [HttpPost("gerar")]
    public async Task<ActionResult<PlanejamentoSemanaResponse>> Gerar(
        [FromQuery] DateOnly? semanaInicio = null,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GerarPlanejamentoRequest? req = null)
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        var dataRef = req?.SemanaInicio ?? semanaInicio ?? DateOnly.FromDateTime(DateTime.Now);
        var semana = PlanejadorSemanal.InicioDaSemana(dataRef);

        var disponibilidades = await _db.Disponibilidades
            .Where(d => d.UsuarioId == usuarioId)
            .OrderBy(d => d.DiaSemana)
            .ThenBy(d => d.HoraInicio)
            .ToListAsync();

        var tarefasAbertas = await _db.Tarefas
            .Where(t => t.UsuarioId == usuarioId
                && t.TarefaPaiId == null
                && (t.Status == StatusTarefa.Pendente || t.Status == StatusTarefa.EmAndamento))
            .ToListAsync();

        var resultado = PlanejadorSemanal.Gerar(usuarioId, semana, DateTime.Now, disponibilidades, tarefasAbertas);

        await using var transacao = await _db.Database.BeginTransactionAsync();
        await _db.PlanejamentoItens
            .Where(p => p.UsuarioId == usuarioId && p.SemanaInicio == semana)
            .ExecuteDeleteAsync();

        if (resultado.Itens.Count > 0)
            _db.PlanejamentoItens.AddRange(resultado.Itens);

        await _db.SaveChangesAsync();
        await transacao.CommitAsync();

        return await MontarResposta(usuarioId, semana);
    }

    // US11 - Move uma tarefa para outro bloco da semana (ou tira do bloco com disponibilidadeId = null).
    [HttpPatch("tarefas/{tarefaId:int}")]
    public async Task<ActionResult<PlanejamentoSemanaResponse>> MoverTarefa(int tarefaId, MoverTarefaPlanejamentoRequest req)
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        var tarefa = await _db.Tarefas.FirstOrDefaultAsync(t => t.Id == tarefaId && t.UsuarioId == usuarioId);
        if (tarefa is null)
            return NotFound(new ErroResponse("Tarefa não encontrada."));
        if (tarefa.TarefaPaiId is not null)
            return BadRequest(new ErroResponse("Subtarefas são planejadas junto com a tarefa principal."));
        if (!tarefa.EstaAberta())
            return BadRequest(new ErroResponse("Somente tarefas abertas podem ser movidas no planejamento."));

        var semana = PlanejadorSemanal.InicioDaSemana(req.SemanaInicio ?? DateOnly.FromDateTime(DateTime.Now));
        var agora = DateTime.Now;

        var itensSemana = await _db.PlanejamentoItens
            .Where(p => p.UsuarioId == usuarioId && p.SemanaInicio == semana)
            .OrderBy(p => p.Ordem)
            .ThenBy(p => p.Id)
            .ToListAsync();

        var itemAtual = itensSemana.FirstOrDefault(p => p.TarefaId == tarefaId);
        int? origemId = itemAtual?.DisponibilidadeId;

        if (req.DisponibilidadeId is null)
        {
            if (itemAtual is not null)
            {
                _db.PlanejamentoItens.Remove(itemAtual);
                if (origemId is int idOrigem)
                    await ReajustarBloco(usuarioId, idOrigem, semana, agora, itensSemana, itemAtual);
                await _db.SaveChangesAsync();
            }
            return await MontarResposta(usuarioId, semana);
        }

        int destinoId = req.DisponibilidadeId.Value;
        var blocoDestino = await _db.Disponibilidades
            .FirstOrDefaultAsync(d => d.Id == destinoId && d.UsuarioId == usuarioId);
        if (blocoDestino is null)
            return NotFound(new ErroResponse("Bloco de disponibilidade não encontrado."));

        if (origemId == destinoId)
            return await MontarResposta(usuarioId, semana);

        var janelaDestino = PlanejadorSemanal.CalcularJanela(semana, blocoDestino, agora);

        var itensDestino = itensSemana
            .Where(p => p.DisponibilidadeId == destinoId && p.TarefaId != tarefaId)
            .ToList();
        var idsTarefasDestino = itensDestino.Select(p => p.TarefaId).ToList();
        var tarefasDestino = await _db.Tarefas
            .Where(t => t.UsuarioId == usuarioId && idsTarefasDestino.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        var paresDestino = itensDestino
            .Where(p => tarefasDestino.TryGetValue(p.TarefaId, out var t) && t.EstaAberta())
            .Select(p => (p, tarefasDestino[p.TarefaId]))
            .ToList();

        PlanejadorSemanal.ResultadoEncaixe encaixe;
        try
        {
            encaixe = PlanejadorSemanal.CalcularEncaixeNoBloco(janelaDestino, paresDestino, tarefa);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErroResponse(ex.Message));
        }

        for (int i = 0; i < encaixe.ExistentesAtualizados.Count; i++)
        {
            var slot = encaixe.ExistentesAtualizados[i];
            slot.Item.AtualizarPosicao(destinoId, janelaDestino.Data, slot.Inicio, slot.Fim, i);
        }

        int novaOrdem = encaixe.ExistentesAtualizados.Count;
        if (itemAtual is not null)
        {
            itemAtual.AtualizarPosicao(destinoId, janelaDestino.Data, encaixe.InicioNova!.Value, encaixe.FimNova!.Value, novaOrdem);
            if (origemId is int idOrigem && idOrigem != destinoId)
                await ReajustarBloco(usuarioId, idOrigem, semana, agora, itensSemana, itemAtual);
        }
        else
        {
            _db.PlanejamentoItens.Add(new PlanejamentoItem(
                usuarioId,
                semana,
                tarefaId,
                destinoId,
                janelaDestino.Data,
                encaixe.InicioNova!.Value,
                encaixe.FimNova!.Value,
                novaOrdem));
        }

        await _db.SaveChangesAsync();
        return await MontarResposta(usuarioId, semana);
    }

    private async Task ReajustarBloco(
        int usuarioId,
        int disponibilidadeId,
        DateOnly semana,
        DateTime agora,
        List<PlanejamentoItem> itensSemana,
        PlanejamentoItem itemRemovido)
    {
        var bloco = await _db.Disponibilidades
            .FirstOrDefaultAsync(d => d.Id == disponibilidadeId && d.UsuarioId == usuarioId);
        if (bloco is null) return;

        var janela = PlanejadorSemanal.CalcularJanela(semana, bloco, agora);
        var restantes = itensSemana
            .Where(p => p.DisponibilidadeId == disponibilidadeId && p.Id != itemRemovido.Id)
            .ToList();
        if (restantes.Count == 0) return;

        var ids = restantes.Select(p => p.TarefaId).ToList();
        var tarefas = await _db.Tarefas
            .Where(t => t.UsuarioId == usuarioId && ids.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

        // Se o bloco já começou, mantém o início a partir de onde o bloco ainda permite.
        var cursor = janela.Passado ? janela.InicioBloco : janela.InicioEfetivo;
        int ordem = 0;
        foreach (var item in restantes)
        {
            if (!tarefas.TryGetValue(item.TarefaId, out var t) || !t.EstaAberta()) continue;
            var fim = cursor.AddMinutes(t.EstimativaMin);
            if (fim <= janela.FimBloco && fim.ToUniversalTime() <= t.Prazo.ToUniversalTime())
            {
                item.AtualizarPosicao(disponibilidadeId, janela.Data, TimeOnly.FromDateTime(cursor), TimeOnly.FromDateTime(fim), ordem++);
                cursor = fim;
            }
            else
            {
                _db.PlanejamentoItens.Remove(item);
            }
        }
    }

    private async Task<PlanejamentoSemanaResponse> MontarResposta(int usuarioId, DateOnly semana)
    {
        var agora = DateTime.Now;
        var disponibilidades = await _db.Disponibilidades
            .Where(d => d.UsuarioId == usuarioId)
            .OrderBy(d => d.DiaSemana)
            .ThenBy(d => d.HoraInicio)
            .ToListAsync();

        var categorias = await _db.Categorias
            .Where(c => c.UsuarioId == usuarioId)
            .ToDictionaryAsync(c => c.Id);

        var tarefasAbertas = await _db.Tarefas
            .Where(t => t.UsuarioId == usuarioId
                && t.TarefaPaiId == null
                && (t.Status == StatusTarefa.Pendente || t.Status == StatusTarefa.EmAndamento))
            .OrderBy(t => t.Prazo)
            .ThenBy(t => t.Id)
            .ToListAsync();
        var tarefasPorId = tarefasAbertas.ToDictionary(t => t.Id);

        var itens = await _db.PlanejamentoItens
            .Where(p => p.UsuarioId == usuarioId && p.SemanaInicio == semana)
            .OrderBy(p => p.Ordem)
            .ThenBy(p => p.Id)
            .ToListAsync();

        var janelas = disponibilidades
            .Select(d => PlanejadorSemanal.CalcularJanela(semana, d, agora))
            .OrderBy(j => j.Data)
            .ThenBy(j => j.Disponibilidade.HoraInicio)
            .ToList();
        var janelasPorId = janelas.ToDictionary(j => j.Disponibilidade.Id);

        // Remove itens que ficaram inválidos após edição de tarefa ou bloco.
        var itensInvalidos = new List<PlanejamentoItem>();
        var itensValidosPorBloco = janelas.ToDictionary(j => j.Disponibilidade.Id, _ => new List<PlanejamentoItem>());
        var tarefasAlocadas = new HashSet<int>();

        foreach (var item in itens)
        {
            if (!tarefasPorId.TryGetValue(item.TarefaId, out var tarefa)
                || !janelasPorId.TryGetValue(item.DisponibilidadeId, out var janela))
            {
                itensInvalidos.Add(item);
                continue;
            }

            var duracaoItem = (int)(item.HoraFim - item.HoraInicio).TotalMinutes;
            var fimItemLocal = janela.Data.ToDateTime(item.HoraFim, DateTimeKind.Local);
            bool cabeNoBloco = item.Data == janela.Data
                && item.HoraInicio >= janela.Disponibilidade.HoraInicio
                && item.HoraFim <= janela.Disponibilidade.HoraFim
                && duracaoItem == tarefa.EstimativaMin
                && fimItemLocal.ToUniversalTime() <= tarefa.Prazo.ToUniversalTime();

            if (!cabeNoBloco)
            {
                itensInvalidos.Add(item);
                continue;
            }

            itensValidosPorBloco[item.DisponibilidadeId].Add(item);
            tarefasAlocadas.Add(item.TarefaId);
        }

        if (itensInvalidos.Count > 0)
        {
            _db.PlanejamentoItens.RemoveRange(itensInvalidos);
            await _db.SaveChangesAsync();
        }

        var blocosResposta = new List<BlocoPlanejadoResponse>();
        int tempoLivreTotalMin = 0;
        int tempoPlanejadoMin = 0;

        foreach (var janela in janelas)
        {
            var bloco = janela.Disponibilidade;
            var itensBloco = itensValidosPorBloco[bloco.Id];
            var tarefasBloco = new List<TarefaPlanejadaResponse>();
            int ocupadoMin = 0;

            foreach (var item in itensBloco)
            {
                var t = tarefasPorId[item.TarefaId];
                categorias.TryGetValue(t.CategoriaId, out var cat);
                tarefasBloco.Add(new TarefaPlanejadaResponse(
                    t.Id,
                    t.Titulo,
                    t.CategoriaId,
                    cat?.Nome ?? string.Empty,
                    cat?.Cor ?? "#9AA6B2",
                    t.Prazo,
                    t.EstimativaMin,
                    t.Status,
                    item.HoraInicio,
                    item.HoraFim));
                ocupadoMin += t.EstimativaMin;
            }

            int livreMin = janela.Passado ? 0 : Math.Max(0, janela.MinutosDisponiveis - ocupadoMin);
            tempoLivreTotalMin += janela.MinutosDisponiveis;
            tempoPlanejadoMin += ocupadoMin;

            blocosResposta.Add(new BlocoPlanejadoResponse(
                bloco.Id,
                bloco.DiaSemana,
                janela.Data,
                bloco.HoraInicio,
                bloco.HoraFim,
                janela.DuracaoTotalMin,
                ocupadoMin,
                livreMin,
                janela.Passado,
                tarefasBloco));
        }

        var semEspacoResposta = new List<TarefaSemEspacoResponse>();
        foreach (var tarefa in tarefasAbertas)
        {
            if (tarefasAlocadas.Contains(tarefa.Id)) continue;
            categorias.TryGetValue(tarefa.CategoriaId, out var cat);
            var motivo = PlanejadorSemanal.MotivoSemEspaco(tarefa, janelas, agora);
            semEspacoResposta.Add(new TarefaSemEspacoResponse(
                tarefa.Id,
                tarefa.Titulo,
                tarefa.CategoriaId,
                cat?.Nome ?? string.Empty,
                cat?.Cor ?? "#9AA6B2",
                tarefa.Prazo,
                tarefa.EstimativaMin,
                tarefa.Status,
                motivo));
        }

        return new PlanejamentoSemanaResponse(
            semana,
            semana.AddDays(6),
            tempoLivreTotalMin,
            tempoPlanejadoMin,
            blocosResposta,
            semEspacoResposta);
    }
}
