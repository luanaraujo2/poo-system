using GestaoTempo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestaoTempo.Api.Data;

// Só em desenvolvimento: cria um usuário de teste (a classe Usuario já cria as 5 categorias padrão).
// Login: teste@sgt.local / senha 12345678.
// Nos testes do .http também dá para usar o cabeçalho "X-Usuario-Id: 1" no lugar do token.
public static class DadosDeTeste
{
    public const string Email = "teste@sgt.local";
    public const string Senha = "12345678";

    public static async Task CriarAsync(AppDbContext db)
    {
        if (await db.Usuarios.AnyAsync()) return;

        db.Usuarios.Add(new Usuario("Usuário de teste", Email, BCrypt.Net.BCrypt.HashPassword(Senha)));
        await db.SaveChangesAsync();
    }
}
