namespace GestaoTempo.Api.Dtos;

// US14 - Relatório de desempenho do usuário.
public record ResumoPrazosResponse(
    int TotalTarefas,
    int Concluidas,
    int EntreguesNoPrazo,
    int EntreguesAtrasadas,
    int AbertasNoPrazo,
    int AbertasAtrasadas,
    int Canceladas,
    double TaxaPontualidadePercentual);

public record ComparacaoTempoResumoResponse(
    int TarefasConcluidas,
    int TempoEstimadoTotalMin,
    int TempoRealTotalMin,
    int DiferencaMin,
    double? DesvioPercentual);

public record ComparacaoTempoTarefaResponse(
    int TarefaId,
    string Titulo,
    string Categoria,
    string CategoriaCor,
    int EstimativaMin,
    int TempoRealMin,
    int DiferencaMin,
    bool NoPrazo,
    DateTime ConcluidoEm);

public record DistribuicaoCategoriaResponse(
    int CategoriaId,
    string Nome,
    string Cor,
    int Quantidade,
    double Percentual,
    int Concluidas,
    int Abertas,
    int Atrasadas,
    int TempoEstimadoMin,
    int TempoRealMin);

public record RelatorioDesempenhoResponse(
    ResumoPrazosResponse Prazos,
    ComparacaoTempoResumoResponse TempoResumo,
    IReadOnlyList<ComparacaoTempoTarefaResponse> TempoPorTarefa,
    IReadOnlyList<DistribuicaoCategoriaResponse> PorCategoria,
    DateOnly? De,
    DateOnly? Ate);
