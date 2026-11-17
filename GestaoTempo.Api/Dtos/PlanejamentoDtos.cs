using GestaoTempo.Domain.Enums;

namespace GestaoTempo.Api.Dtos;

// US11 - Planejamento da semana.
public record GerarPlanejamentoRequest(DateOnly? SemanaInicio = null);

public record MoverTarefaPlanejamentoRequest(int? DisponibilidadeId, DateOnly? SemanaInicio = null);

public record TarefaPlanejadaResponse(
    int TarefaId,
    string Titulo,
    int CategoriaId,
    string Categoria,
    string CategoriaCor,
    DateTime Prazo,
    int EstimativaMin,
    StatusTarefa Status,
    TimeOnly InicioPrevisto,
    TimeOnly FimPrevisto);

public record BlocoPlanejadoResponse(
    int DisponibilidadeId,
    DiaSemana DiaSemana,
    DateOnly Data,
    TimeOnly HoraInicio,
    TimeOnly HoraFim,
    int DuracaoTotalMin,
    int TempoOcupadoMin,
    int TempoLivreMin,
    bool Passado,
    IReadOnlyList<TarefaPlanejadaResponse> Tarefas);

public record TarefaSemEspacoResponse(
    int TarefaId,
    string Titulo,
    int CategoriaId,
    string Categoria,
    string CategoriaCor,
    DateTime Prazo,
    int EstimativaMin,
    StatusTarefa Status,
    string Motivo);

public record PlanejamentoSemanaResponse(
    DateOnly SemanaInicio,
    DateOnly SemanaFim,
    int TempoLivreTotalMin,
    int TempoPlanejadoMin,
    IReadOnlyList<BlocoPlanejadoResponse> Blocos,
    IReadOnlyList<TarefaSemEspacoResponse> SemEspaco);
