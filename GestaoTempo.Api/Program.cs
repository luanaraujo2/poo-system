using GestaoTempo.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Banco PostgreSQL (connection string em appsettings.Development.json)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Sgt")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Cria/atualiza as tabelas e o usuário de teste ao iniciar.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DadosDeTeste.CriarAsync(db);
}

app.Run();
