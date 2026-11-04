namespace GestaoTempo.Api.Dtos;

// Dados que a tela "Nova tarefa" envia (US03). A edição (US05) usa os mesmos campos.
// Os campos ficam opcionais aqui para a API devolver a mensagem certa quando faltar algum.
// TarefaPaiId (US06): ao cadastrar, cria a tarefa já como subtarefa dessa tarefa. Na edição é ignorado.
public record CriarTarefaRequest(
    string? Titulo,
    string? Descricao,
    DateTime? Prazo,
    int EstimativaMin,
    int CategoriaId,
    Criticidade? Criticidade,
    Complexidade? Complexidade,
    Prioridade? Prioridade,
    int? TarefaPaiId = null);

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
    int SubtarefasConcluidas);

// Tarefas = quantas tarefas usam a categoria (a tela de categorias mostra e pede para onde movê-las antes de excluir).
public record CategoriaResponse(int Id, string Nome, string? Cor, bool Padrao, int Tarefas);

// US04 - criar e editar categoria. Cor no formato #RRGGBB (opcional ao criar).
public record SalvarCategoriaRequest(string? Nome, string? Cor);

public record ErroResponse(string Mensagem);
