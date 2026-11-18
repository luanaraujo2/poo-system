using GestaoTempo.Domain.Entities;

namespace GestaoTempo.Api.Dtos;

// Dados que a tela "Nova tarefa" envia (US03). A edição (US05) usa os mesmos campos.
// Os campos ficam opcionais aqui para a API devolver a mensagem certa quando faltar algum.
// TarefaPaiId (US06): ao cadastrar, cria a tarefa já como subtarefa dessa tarefa. Na edição é ignorado.
// LembreteMin (US13): minutos antes do prazo. Sem o campo, vale 24 horas; null deixa a tarefa sem lembrete.
public record CriarTarefaRequest(
    string? Titulo,
    string? Descricao,
    DateTime? Prazo,
    int EstimativaMin,
    int CategoriaId,
    Criticidade? Criticidade,
    Complexidade? Complexidade,
    Prioridade? Prioridade,
    int? TarefaPaiId = null,
    int? LembreteMin = Tarefa.LembretePadraoMin);

// US07 - Atualizar status. TempoRealMin é obrigatório só para concluir.
public record AlterarStatusRequest(StatusTarefa? Status, int? TempoRealMin);

public record TarefaResponse(
    int Id,
    string Titulo,
    string? Descricao,
    DateTime Prazo,
    int EstimativaMin,
    int CategoriaId,
    string Categoria,
    Criticidade Criticidade,
    Complexidade Complexidade,
    Prioridade Prioridade,
    StatusTarefa Status,
    DateTime CriadoEm,
    IReadOnlyList<int> SubtarefasIds,
    int? TarefaPaiId,
    DateTime? ConcluidoEm,
    int TempoRealMin,
    int SubtarefasMin,
    int SubtarefasConcluidas,
    int? LembreteMin);

// US12 - Uma mudança de status. StatusAnterior vem nulo no registro do cadastro.
public record HistoricoStatusResponse(StatusTarefa? StatusAnterior, StatusTarefa StatusNovo, DateTime AlteradoEm);

// US12 - Visão geral do histórico por tarefa do usuário.
public record TarefaHistoricoResponse(
    int TarefaId,
    string Titulo,
    StatusTarefa StatusAtual,
    string Categoria,
    string CategoriaCor,
    DateTime Prazo,
    DateTime CriadoEm,
    DateTime? ConcluidoEm,
    int? TarefaPaiId,
    IReadOnlyList<HistoricoStatusResponse> Historico);

// US13 - Lembrete que está valendo agora (entre LembrarEm e o prazo).
public record LembreteResponse(int TarefaId, string Titulo, DateTime Prazo, int LembreteMin, DateTime LembrarEm);

// US09 - Comparação da carga estimada com a disponibilidade da semana atual.
public record SobrecargaSemanaResponse(
    DateOnly InicioSemana,
    DateOnly FimSemana,
    int TempoEstimadoMin,
    int TempoLivreMin,
    int SobrecargaMin);

// Tarefas = quantas tarefas usam a categoria (a tela de categorias mostra e pede para onde movê-las antes de excluir).
public record CategoriaResponse(int Id, string Nome, string? Cor, bool Padrao, int Tarefas);

// US04 - criar e editar categoria. Cor no formato #RRGGBB (opcional ao criar).
public record SalvarCategoriaRequest(string? Nome, string? Cor);

public record ErroResponse(string Mensagem);
