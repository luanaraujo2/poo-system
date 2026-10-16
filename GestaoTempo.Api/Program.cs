using System.Text.Json.Serialization;
using GestaoTempo.Api.Data;
using GestaoTempo.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Banco PostgreSQL (connection string em appsettings.Development.json)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Sgt")));

builder.Services.AddControllers()
    // Enums trafegam como texto no JSON (ex.: "Alta", "Urgente_Importante").
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UsuarioAtual>();
builder.Services.AddOpenApi();

// Libera o front-end (Vue/Vite roda em http://localhost:5173)
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Cria/atualiza as tabelas e o usuário de teste ao iniciar.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DadosDeTeste.CriarAsync(db);
}

app.UseCors();
app.MapControllers();

app.Run();
