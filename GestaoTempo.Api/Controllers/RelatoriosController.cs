using GestaoTempo.Api.Data;
using GestaoTempo.Api.Dtos;
using GestaoTempo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoTempo.Api.Controllers;

[ApiController]
[Route("api/relatorios")]
public class RelatoriosController : ControllerAutenticado
{
    private readonly AppDbContext _db;

    public RelatoriosController(AppDbContext db, UsuarioAtual usuarioAtual) : base(usuarioAtual)
    {
        _db = db;
    }

    // US14 - Relatório de desempenho: prazos, tempo estimado x real e distribuição por categoria.
    [HttpGet("desempenho")]
    public async Task<ActionResult<RelatorioDesempenhoResponse>> Desempenho()
    {
        if (UsuarioAtual.Id is not int usuarioId)
            return NaoIdentificado();

        var agora = DateTime.Now;

        var categorias = await _db.Categorias
            .Where(c => c.UsuarioId == usuarioId)
            .OrderBy(c => c.Id)
            .ToListAsync();
        var categoriasPorId = categorias.ToDictionary(c => c.Id);

        // Usa apenas tarefas principais para não contar o tempo das subtarefas duas vezes.
        var tarefas = await _db.Tarefas
            .Where(t => t.UsuarioId == usuarioId && t.TarefaPaiId == null)
            .ToListAsync();

        var concluidas = tarefas.Where(t => t.Status == StatusTarefa.Concluida).ToList();
        int entreguesNoPrazo = concluidas.Count(t => t.ConcluidaNoPrazo());
        int entreguesAtrasadas = concluidas.Count(t => !t.ConcluidaNoPrazo());
        int abertasAtrasadas = tarefas.Count(t => t.EstaAberta() && t.EntregueComAtraso(agora));
        int abertasNoPrazo = tarefas.Count(t => t.EstaAberta() && !t.EntregueComAtraso(agora));
        int canceladas = tarefas.Count(t => t.Status == StatusTarefa.Cancelada);
        double taxaPontualidade = concluidas.Count > 0
            ? Math.Round(entreguesNoPrazo * 100.0 / concluidas.Count, 1)
            : 0;

        var resumoPrazos = new ResumoPrazosResponse(
            tarefas.Count,
            concluidas.Count,
            entreguesNoPrazo,
            entreguesAtrasadas,
            abertasNoPrazo,
            abertasAtrasadas,
            canceladas,
            taxaPontualidade);

        int tempoEstimadoTotal = concluidas.Sum(t => t.EstimativaMin);
        int tempoRealTotal = concluidas.Sum(t => t.TempoRealMin);
        int diferencaTotal = tempoRealTotal - tempoEstimadoTotal;
        double? desvioPercentual = tempoEstimadoTotal > 0
            ? Math.Round(diferencaTotal * 100.0 / tempoEstimadoTotal, 1)
            : null;

        var tempoResumo = new ComparacaoTempoResumoResponse(
            concluidas.Count,
            tempoEstimadoTotal,
            tempoRealTotal,
            diferencaTotal,
            desvioPercentual);

        var tempoPorTarefa = concluidas
            .OrderByDescending(t => t.ConcluidoEm ?? t.CriadoEm)
            .Select(t =>
            {
                categoriasPorId.TryGetValue(t.CategoriaId, out var cat);
                return new ComparacaoTempoTarefaResponse(
                    t.Id,
                    t.Titulo,
                    cat?.Nome ?? string.Empty,
                    cat?.Cor ?? "#9AA6B2",
                    t.EstimativaMin,
                    t.TempoRealMin,
                    t.TempoRealMin - t.EstimativaMin,
                    t.ConcluidaNoPrazo(),
                    t.ConcluidoEm ?? t.CriadoEm);
            })
            .ToList();

        var porCategoria = categorias
            .Select(c =>
            {
                var grupo = tarefas.Where(t => t.CategoriaId == c.Id).ToList();
                int quantidade = grupo.Count;
                double percentual = tarefas.Count > 0
                    ? Math.Round(quantidade * 100.0 / tarefas.Count, 1)
                    : 0;
                return new DistribuicaoCategoriaResponse(
                    c.Id,
                    c.Nome,
                    c.Cor,
                    quantidade,
                    percentual,
                    grupo.Count(t => t.Status == StatusTarefa.Concluida),
                    grupo.Count(t => t.EstaAberta()),
                    grupo.Count(t => t.EntregueComAtraso(agora)),
                    grupo.Sum(t => t.EstimativaMin),
                    grupo.Where(t => t.Status == StatusTarefa.Concluida).Sum(t => t.TempoRealMin));
            })
            .OrderByDescending(c => c.Quantidade)
            .ThenBy(c => c.CategoriaId)
            .ToList();

        return new RelatorioDesempenhoResponse(resumoPrazos, tempoResumo, tempoPorTarefa, porCategoria);
    }
}
