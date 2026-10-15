public class Categoria
{
    public int Id {get; private set;}
    public int UsuarioId {get; private set;}
    public string Nome {get; private set;}
    public string Cor {get; private set;}
    public bool Padrao {get; private set;}

    public Categoria(string nome, string cor, int usuarioId, bool padrao = false)
    {
        
        if (string.IsNullOrWhiteSpace(nome) || nome.Length < 3)
        {
            throw new ArgumentException("O nome da categoria deve ter pelo menos 3 caracteres.");
        }
        Nome = nome;
        Cor = cor;
        UsuarioId = usuarioId;
        Padrao = padrao;
    }

    public void Renomear(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Length < 3)
        {
            throw new ArgumentException("O nome da categoria deve ter pelo menos 3 caracteres.");
        }
        Nome = nome;
    }
}