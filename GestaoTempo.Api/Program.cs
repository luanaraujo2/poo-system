using System.Text.Json.Serialization;
using GestaoTempo.Api.Data;
using GestaoTempo.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Banco PostgreSQL (connection string em appsettings.Development.json)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Sgt")));

builder.Services.AddControllers()
    // Enums trafegam como texto no JSON (ex.: "Alta", "Urgente_Importante").
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UsuarioAtual>();
builder.Services.AddSingleton<GeradorDeToken>();

// Login por token (US02): o front manda "Authorization: Bearer <token>" em cada requisição.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Emissor"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Emissor"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = GeradorDeToken.ChaveDeAssinatura(builder.Configuration),
    });
builder.Services.AddAuthorization();
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
