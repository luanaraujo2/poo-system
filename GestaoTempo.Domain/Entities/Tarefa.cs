namespace GestaoTempo.Domain.Entities;

public class Tarefa
{
    public int Id {get; private set;}
    public int UsuarioId { get; private set; }
    public int CategoriaId { get; private set; }
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

    public void AlterarStatus(StatusTarefa status)
    {
        Status = status;
        if (status == StatusTarefa.Concluida)
        {
            ConcluidoEm = DateTime.Now;
        }
    }
    /*public void adicionarSubtarefa()
    {
        
    }
    estimativaTotal()
    {
        
    }
    estaAtradada()
    {
        
    }
    podeConcluir()
    {
        
    }
    */
}
