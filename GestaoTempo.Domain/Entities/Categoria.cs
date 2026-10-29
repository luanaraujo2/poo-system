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

    // US04 - cor no formato #RRGGBB, a mesma usada nas categorias padrão.
    public void AlterarCor(string cor)
    {
        if (!CorValida(cor))
            throw new ArgumentException("A cor deve estar no formato #RRGGBB.");
        Cor = cor;
    }

    public static bool CorValida(string? cor) =>
        cor is not null && System.Text.RegularExpressions.Regex.IsMatch(cor, "^#[0-9A-Fa-f]{6}$");

    public void Renomear(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Length < 3)
        {
            throw new ArgumentException("O nome da categoria deve ter pelo menos 3 caracteres.");
        }
        Nome = nome;
    }
}