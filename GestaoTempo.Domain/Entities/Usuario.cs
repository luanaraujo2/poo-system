namespace GestaoTempo.Domain.Entities;

public class Usuario
{
    public int Id {get; private set;}
    public string Nome {get; private set;}
    public string Email {get; private set;}
    public string SenhaHash {get; private set;}
    public DateTime CriadoEm {get; private set;}
    private readonly List<Categoria> _categorias = new List<Categoria>();
    public IReadOnlyCollection<Categoria> Categorias => _categorias.AsReadOnly();

    public Usuario(string nome, string email, string senhaHash)
    {
        Nome = nome;
        Email = email;
        SenhaHash = senhaHash;
        CriadoEm = DateTime.Now;
        if (string.IsNullOrWhiteSpace(nome) || nome.Length < 3)
        {
            throw new ArgumentException("O nome do usuário deve ter pelo menos 3 caracteres.");
        }
        else if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
        {
            throw new ArgumentException("O email do usuário deve ser válido.");
        }
        else if (string.IsNullOrWhiteSpace(senhaHash) || senhaHash.Length < 6)
        {
            throw new ArgumentException("A senha do usuário deve ter pelo menos 8 caracteres.");
        }
        CriarCategoriasPadrao();
    }

    private void CriarCategoriasPadrao()
    {
        _categorias.Add(new Categoria("Trabalho", "#FF5733", Id, true));
        _categorias.Add(new Categoria("Estudos", "#33FF57", Id, true));
        _categorias.Add(new Categoria("Pessoal", "#3357FF", Id, true));
        _categorias.Add(new Categoria("Familia", "#FF33A1", Id, true));
        _categorias.Add(new Categoria("Relacionamentos", "#33FFF5", Id, true));
    }

}