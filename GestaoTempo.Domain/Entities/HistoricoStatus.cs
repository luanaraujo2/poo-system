namespace GestaoTempo.Domain.Entities;

// US12 - Cada mudança de status de uma tarefa fica registrada, com data e hora.
// Quem cria os registros é a própria Tarefa (no construtor e em AlterarStatus).
public class HistoricoStatus
{
    public int Id { get; private set; }
    public int TarefaId { get; private set; }
    public StatusTarefa? StatusAnterior { get; private set; }
    public StatusTarefa StatusNovo { get; private set; }
    public DateTime AlteradoEm { get; private set; }

    public HistoricoStatus(StatusTarefa? statusAnterior, StatusTarefa statusNovo)
    {
        if (statusAnterior == statusNovo)
            throw new ArgumentException("O histórico só registra mudanças de status.");
        StatusAnterior = statusAnterior;
        StatusNovo = statusNovo;
        AlteradoEm = DateTime.Now;
    }

    private HistoricoStatus() { }
}
