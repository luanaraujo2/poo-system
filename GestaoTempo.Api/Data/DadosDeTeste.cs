using GestaoTempo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestaoTempo.Api.Data;

// Só em desenvolvimento: cria um usuário de teste (a classe Usuario já cria as 5 categorias padrão),
// para dar para testar a US03 antes do cadastro e do login (US01 e US02) ficarem prontos.
// Use o cabeçalho "X-Usuario-Id: 1" nas requisições.
public static class DadosDeTeste
{
    public static async Task CriarAsync(AppDbContext db)
    {
        if (await db.Usuarios.AnyAsync()) return;

        db.Usuarios.Add(new Usuario("Usuário de teste", "teste@sgt.local", "senha-de-teste"));
        await db.SaveChangesAsync();
    }
}
