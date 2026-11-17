namespace GestaoTempo.Domain.Entities;

// US11 - Item do planejamento semanal: liga uma tarefa a um bloco de disponibilidade da semana.
public class PlanejamentoItem
{
    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public DateOnly SemanaInicio { get; private set; }
    public int TarefaId { get; private set; }
    public int DisponibilidadeId { get; private set; }
    public DateOnly Data { get; private set; }
    public TimeOnly HoraInicio { get; private set; }
    public TimeOnly HoraFim { get; private set; }
    public int Ordem { get; private set; }

    public PlanejamentoItem(
        int usuarioId,
        DateOnly semanaInicio,
        int tarefaId,
        int disponibilidadeId,
        DateOnly data,
        TimeOnly horaInicio,
        TimeOnly horaFim,
        int ordem)
    {
        if (usuarioId <= 0)
            throw new ArgumentException("Usuário inválido.");
        if (tarefaId <= 0)
            throw new ArgumentException("Tarefa inválida.");
        if (disponibilidadeId <= 0)
            throw new ArgumentException("Bloco de disponibilidade inválido.");
        if (horaFim <= horaInicio)
            throw new ArgumentException("O horário final do item deve ser maior que o inicial.");
        if (ordem < 0)
            throw new ArgumentException("A ordem do item não pode ser negativa.");

        UsuarioId = usuarioId;
        SemanaInicio = semanaInicio;
        TarefaId = tarefaId;
        DisponibilidadeId = disponibilidadeId;
        Data = data;
        HoraInicio = horaInicio;
        HoraFim = horaFim;
        Ordem = ordem;
    }

    private PlanejamentoItem() { }

    public void AtualizarPosicao(int disponibilidadeId, DateOnly data, TimeOnly horaInicio, TimeOnly horaFim, int ordem)
    {
        if (disponibilidadeId <= 0)
            throw new ArgumentException("Bloco de disponibilidade inválido.");
        if (horaFim <= horaInicio)
            throw new ArgumentException("O horário final do item deve ser maior que o inicial.");
        if (ordem < 0)
            throw new ArgumentException("A ordem do item não pode ser negativa.");

        DisponibilidadeId = disponibilidadeId;
        Data = data;
        HoraInicio = horaInicio;
        HoraFim = horaFim;
        Ordem = ordem;
    }
}
