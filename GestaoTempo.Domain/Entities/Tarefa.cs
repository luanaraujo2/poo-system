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
    // US12 - histórico das mudanças de status.
    private readonly List<HistoricoStatus> _historico = new List<HistoricoStatus>();
    public IReadOnlyCollection<HistoricoStatus> Historico => _historico.AsReadOnly();
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

    // US13 - Lembrete: quantos minutos antes do prazo avisar. Sem valor, a tarefa fica sem lembrete.
    public const int LembretePadraoMin = 24 * 60;
    public const int LembreteMaximoMin = 30 * 24 * 60;
    public int? LembreteMin { get; private set; }
    public DateTime? LembreteDispensadoEm { get; private set; }

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
        LembreteMin = LembretePadraoMin;
        _historico.Add(new HistoricoStatus(null, StatusTarefa.Pendente));
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
        {
            Prazo = prazo;
            // Prazo novo, lembrete novo (US13).
            LembreteDispensadoEm = null;
        }
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
        // US12: só registra quando o status muda de verdade.
        if (status != Status)
            _historico.Add(new HistoricoStatus(Status, status));
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

    // US07 - Concluir: grava a data e a hora (em AlterarStatus) e o tempo real gasto, informado pelo usuário.
    public void Concluir(int tempoRealMin)
    {
        if (tempoRealMin <= 0)
            throw new ArgumentException("Informe o tempo real gasto, maior que zero.");
        AlterarStatus(StatusTarefa.Concluida);
        TempoRealMin = tempoRealMin;
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

    public bool EstaAberta()
    {
        return Status == StatusTarefa.Pendente || Status == StatusTarefa.EmAndamento;
    }

    // US14 - Entregue no prazo: concluída até o momento do prazo.
    public bool ConcluidaNoPrazo()
    {
        return Status == StatusTarefa.Concluida
            && ConcluidoEm is DateTime concluidoEm
            && concluidoEm.ToUniversalTime() <= Prazo.ToUniversalTime();
    }

    // US14 - Atrasada: concluída depois do prazo, ou ainda aberta com o prazo vencido.
    // As datas lidas do banco voltam em UTC, por isso a comparação é feita em UTC.
    public bool EntregueComAtraso(DateTime agora)
    {
        if (Status == StatusTarefa.Concluida)
            return !ConcluidaNoPrazo();
        return EstaAberta() && agora.ToUniversalTime() > Prazo.ToUniversalTime();
    }

    // US13 - Define quantos minutos antes do prazo o usuário quer ser lembrado (null = sem lembrete).
    public void DefinirLembrete(int? minutosAntes)
    {
        if (minutosAntes is int minutos && (minutos <= 0 || minutos > LembreteMaximoMin))
            throw new ArgumentException("O lembrete deve ser de 1 minuto a 30 dias antes do prazo.");
        if (minutosAntes != LembreteMin)
            LembreteDispensadoEm = null;
        LembreteMin = minutosAntes;
    }

    public DateTime? MomentoDoLembrete()
    {
        return LembreteMin is int minutos ? Prazo.AddMinutes(-minutos) : null;
    }

    // US13 - O lembrete aparece entre o momento configurado e o prazo.
    // Tarefa concluída ou cancelada não recebe lembrete.
    public bool DeveLembrar(DateTime agora)
    {
        if (LembreteMin is not int minutos || LembreteDispensadoEm is not null || !EstaAberta())
            return false;
        var agoraUtc = agora.ToUniversalTime();
        var prazoUtc = Prazo.ToUniversalTime();
        return agoraUtc >= prazoUtc.AddMinutes(-minutos) && agoraUtc < prazoUtc;
    }

    public void DispensarLembrete()
    {
        if (!DeveLembrar(DateTime.Now))
            throw new InvalidOperationException("Esta tarefa não tem lembrete para dispensar agora.");
        LembreteDispensadoEm = DateTime.Now;
    }
}
