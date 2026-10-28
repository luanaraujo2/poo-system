namespace GestaoTempo.Domain.Entities;

public class Tarefa
{
    public int Id {get; private set;}
    public int UsuarioId { get; private set; }
    public int CategoriaId { get; private set; }
    public int? TarefaPaiId { get; private set; }
    public Tarefa? TarefaPai { get; private set; }
    private readonly List<Tarefa> _subtarefas = new List<Tarefa>();
    public IReadOnlyCollection<Tarefa> Subtarefas => _subtarefas.AsReadOnly();
    public string Titulo {get; private set;}
    public string Descricao {get; private set;}
    public DateTime Prazo {get; private set;}
    public int EstimativaMin {get; private set;}
    public int TempoRealMin {get; private set;}
    public Criticidade Criticidade {get; private set;}
    public Complexidade Complexidade {get; private set;}
    public Prioridade Prioridade {get; private set;}
    public StatusTarefa Status {get; private set;}
    public DateTime CriadoEm {get; private set;}
    public DateTime? ConcluidoEm {get; private set;}

    public Tarefa(string titulo, string descricao, DateTime prazo, int estimativaMin, Criticidade criticidade, Complexidade complexidade, Prioridade prioridade, int usuarioId, int categoriaId)
    {
        if (string.IsNullOrWhiteSpace(titulo) || titulo.Length < 3)
            throw new ArgumentException("O título da tarefa deve ter pelo menos 3 caracteres.");
        if (estimativaMin <= 0)
            throw new ArgumentException("A estimativa de tempo deve ser maior que zero.");
        if (prazo < DateTime.Now)
            throw new ArgumentException("O prazo da tarefa deve ser uma data futura.");
        Titulo = titulo;
        Descricao = descricao;
        Prazo = prazo;
        EstimativaMin = estimativaMin;
        Criticidade = criticidade;
        Complexidade = complexidade;
        Prioridade = prioridade;
        UsuarioId = usuarioId;
        CategoriaId = categoriaId;
        
        Status = StatusTarefa.Pendente;
        CriadoEm = DateTime.Now;
    }

    // Usado só pelo banco (EF Core) ao ler uma tarefa já gravada.
    // Sem ele, o EF chama o construtor acima, que recusa prazo vencido.
#pragma warning disable CS8618
    private Tarefa() { }
#pragma warning restore CS8618

    // US05 - Editar tarefa. O prazo só é conferido se mudou: uma tarefa atrasada
    // pode ter o título corrigido sem precisar mudar o prazo.
    public void Editar(string titulo, string descricao, DateTime prazo, int estimativaMin, Criticidade criticidade, Complexidade complexidade, Prioridade prioridade)
    {
        if (string.IsNullOrWhiteSpace(titulo) || titulo.Length < 3)
            throw new ArgumentException("O título da tarefa deve ter pelo menos 3 caracteres.");
        if (estimativaMin <= 0)
            throw new ArgumentException("A estimativa de tempo deve ser maior que zero.");
        bool prazoMudou = prazo.ToUniversalTime() != Prazo.ToUniversalTime();
        if (prazoMudou && prazo < DateTime.Now)
            throw new ArgumentException("O prazo da tarefa deve ser uma data futura.");
        if (estimativaMin < SomarEstimativasSubtarefas())
            throw new InvalidOperationException("A estimativa não pode ficar menor que a soma das subtarefas.");

        Titulo = titulo;
        Descricao = descricao;
        if (prazoMudou)
            Prazo = prazo;
        EstimativaMin = estimativaMin;
        Criticidade = criticidade;
        Complexidade = complexidade;
        Prioridade = prioridade;
    }

    // US04 - usada ao editar a tarefa e ao excluir uma categoria (as tarefas vão para outra).
    public void TrocarCategoria(int categoriaId)
    {
        if (categoriaId <= 0)
            throw new ArgumentException("Categoria inválida.");
        CategoriaId = categoriaId;
    }

    public void AlterarStatus(StatusTarefa status)
    {
        if (Status == StatusTarefa.Concluida)
        {
            throw new InvalidOperationException("Não é possivel mudar o status de uma tarefa já concluída para outro status.");
        }
        if (status == StatusTarefa.Concluida && !PodeConcluir())
        {
            throw new InvalidOperationException("Não é possível concluir a tarefa enquanto houver subtarefas pendentes.");
        }
        Status = status;
        if (Status == StatusTarefa.Concluida)
        {
            ConcluidoEm = DateTime.Now;
        }
        else
        {
            ConcluidoEm = null;
        }
    }

    public void AdicionarSubtarefa(Tarefa subtarefa)
    {
        if (subtarefa is null)
            throw new ArgumentNullException(nameof(subtarefa));
        if (Status == StatusTarefa.Concluida)
            throw new InvalidOperationException("Não é possível adicionar subtarefas a uma tarefa concluída.");
        if (subtarefa == this)
            throw new InvalidOperationException("Uma tarefa não pode ser subtarefa dela mesma.");
        if (subtarefa.UsuarioId != UsuarioId)
            throw new InvalidOperationException("A tarefa-pai e a subtarefa devem pertencer ao mesmo usuário.");
        if (subtarefa.TarefaPaiId is not null || subtarefa.TarefaPai is not null)
            throw new InvalidOperationException("Esta tarefa já está vinculada a uma tarefa-pai.");
        for (Tarefa? ancestral = this; ancestral is not null; ancestral = ancestral.TarefaPai)
        {
            if (ancestral == subtarefa)
                throw new InvalidOperationException("Essa relação criaria um ciclo entre tarefas.");
        }
        if (_subtarefas.Contains(subtarefa))
            throw new InvalidOperationException("Esta tarefa já foi adicionada como subtarefa.");

        int novaSoma = SomarEstimativasSubtarefas() + subtarefa.EstimativaMin;
        if (novaSoma > EstimativaMin)
            throw new InvalidOperationException("As subtarefas ultrapassam a estimativa da tarefa.");
        subtarefa.TarefaPai = this;
        _subtarefas.Add(subtarefa);
    }

    public int SomarEstimativasSubtarefas()
    {
        return _subtarefas.Sum(sub => sub.EstimativaMin);
    }

    public int MinutosNaoDistribuidos()
    {
        return EstimativaMin - SomarEstimativasSubtarefas();
    }

    public bool EstimativaFoiDivididaCorretamente()
    {
        if (_subtarefas.Count == 0)
            return true;
        return MinutosNaoDistribuidos() == 0;
    }

    public bool EstaAtrasada()
    {
        return DateTime.Now > Prazo && Status != StatusTarefa.Concluida;
    }
    public bool PodeConcluir()
    {
        return _subtarefas.All(sub => sub.Status == StatusTarefa.Concluida);
    }
}
