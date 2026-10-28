namespace GestaoTempo.Api.Dtos;

// Dados que a tela "Nova tarefa" envia (US03). A edição (US05) usa os mesmos campos.
// Os campos ficam opcionais aqui para a API devolver a mensagem certa quando faltar algum.
public record CriarTarefaRequest(
    string? Titulo,
    string? Descricao,
    DateTime? Prazo,
    int EstimativaMin,
    int CategoriaId,
    Criticidade? Criticidade,
    Complexidade? Complexidade,
    Prioridade? Prioridade);

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
    IReadOnlyList<int> SubtarefasIds);

// Tarefas = quantas tarefas usam a categoria (a tela de categorias mostra e pede para onde movê-las antes de excluir).
public record CategoriaResponse(int Id, string Nome, string? Cor, bool Padrao, int Tarefas);

// US04 - criar e editar categoria. Cor no formato #RRGGBB (opcional ao criar).
public record SalvarCategoriaRequest(string? Nome, string? Cor);

public record ErroResponse(string Mensagem);
