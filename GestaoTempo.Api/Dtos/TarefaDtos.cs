namespace GestaoTempo.Api.Dtos;

// Dados que a tela "Nova tarefa" envia (US03).
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

public record CategoriaResponse(int Id, string Nome, string? Cor, bool Padrao);

public record ErroResponse(string Mensagem);
