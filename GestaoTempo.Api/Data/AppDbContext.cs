using GestaoTempo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GestaoTempo.Api.Data;

// Mapeamento das classes do Domain para as tabelas do DER (Sprint 1).
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Tarefa> Tarefas => Set<Tarefa>();
    public DbSet<Disponibilidade> Disponibilidades => Set<Disponibilidade>();
    public DbSet<HistoricoStatus> HistoricosStatus => Set<HistoricoStatus>();
    public DbSet<PlanejamentoItem> PlanejamentoItens => Set<PlanejamentoItem>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // O PostgreSQL (timestamptz) só aceita data em UTC.
        // As classes usam DateTime.Now, então a conversão é feita aqui na gravação.
        configurationBuilder.Properties<DateTime>().HaveConversion<DataUtcConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<DataUtcNulavelConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // citext: texto sem diferenciar maiúsculas de minúsculas (e-mail único e login, US01/US02).
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuario");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("id_usuario");
            e.Property(u => u.Nome).HasColumnName("nome").HasMaxLength(100).IsRequired();
            e.Property(u => u.Email).HasColumnName("email").HasColumnType("citext").HasMaxLength(150).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.SenhaHash).HasColumnName("senha_hash").HasMaxLength(255).IsRequired();
            e.Property(u => u.CriadoEm).HasColumnName("criado_em");

            // A lista de categorias do usuário fica no campo privado _categorias.
            e.HasMany(u => u.Categorias)
                .WithOne()
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(u => u.Categorias).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Categoria>(e =>
        {
            e.ToTable("categoria");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id_categoria");
            e.Property(c => c.UsuarioId).HasColumnName("id_usuario");
            e.Property(c => c.Nome).HasColumnName("nome").HasMaxLength(50).IsRequired();
            e.Property(c => c.Cor).HasColumnName("cor").HasMaxLength(7);
            e.Property(c => c.Padrao).HasColumnName("padrao");

            // Um usuário não pode ter duas categorias com o mesmo nome.
            e.HasIndex(c => new { c.UsuarioId, c.Nome }).IsUnique();
        });

        modelBuilder.Entity<Disponibilidade>(e =>
        {
            e.ToTable("disponibilidade", t =>
            {
                t.HasCheckConstraint("CK_disponibilidade_horario_valido", "hora_fim > hora_inicio");
                t.HasCheckConstraint("CK_disponibilidade_dia_valido",
                    "dia_semana IN ('Segunda', 'Terca', 'Quarta', 'Quinta', 'Sexta', 'Sabado', 'Domingo')");
            });
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).HasColumnName("id_disponibilidade");
            e.Property(d => d.UsuarioId).HasColumnName("id_usuario");
            e.Property(d => d.DiaSemana).HasColumnName("dia_semana").HasConversion<string>().HasMaxLength(10);
            e.Property(d => d.HoraInicio).HasColumnName("hora_inicio");
            e.Property(d => d.HoraFim).HasColumnName("hora_fim");
            e.HasIndex(d => new { d.UsuarioId, d.DiaSemana, d.HoraInicio });
            e.HasOne<Usuario>().WithMany().HasForeignKey(d => d.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Tarefa>(e =>
        {
            // O banco também recusa dados fora das regras (além da validação das classes do Domain).
            // Se um valor novo for criado nos enums, atualize o CHECK correspondente.
            e.ToTable("tarefa", t =>
            {
                t.HasCheckConstraint("CK_tarefa_estimativa_positiva", "estimativa_min > 0");
                t.HasCheckConstraint("CK_tarefa_nao_pode_ser_pai_de_si_mesma", "id_tarefa_pai IS NULL OR id_tarefa_pai <> id_tarefa");
                t.HasCheckConstraint("CK_tarefa_titulo_preenchido", "char_length(btrim(titulo)) > 0");
                t.HasCheckConstraint("CK_tarefa_tempo_real_nao_negativo", "tempo_real_min >= 0");
                t.HasCheckConstraint("CK_tarefa_status_valido",
                    "status IN ('Pendente', 'EmAndamento', 'Concluida', 'Cancelada')");
                t.HasCheckConstraint("CK_tarefa_criticidade_valida",
                    "criticidade IN ('Baixa', 'Media', 'Alta', 'Critica')");
                t.HasCheckConstraint("CK_tarefa_complexidade_valida",
                    "complexidade IN ('Baixa', 'Media', 'Alta')");
                t.HasCheckConstraint("CK_tarefa_prioridade_valida",
                    "prioridade IN ('Urgente_Importante', 'Importante_NaoUrgente', 'Urgente_NaoImportante', 'NaoUrgente_NaoImportante')");
                t.HasCheckConstraint("CK_tarefa_lembrete_valido",
                    "lembrete_min IS NULL OR lembrete_min BETWEEN 1 AND 43200");
            });
            e.HasKey(t => t.Id);
            e.HasAlternateKey(t => new { t.Id, t.UsuarioId });
            e.Property(t => t.TarefaPaiId).HasColumnName("id_tarefa_pai");
            e.Property(t => t.Id).HasColumnName("id_tarefa");
            e.Property(t => t.UsuarioId).HasColumnName("id_usuario");
            e.Property(t => t.CategoriaId).HasColumnName("id_categoria");
            e.Property(t => t.Titulo).HasColumnName("titulo").HasMaxLength(150).IsRequired();
            e.Property(t => t.Descricao).HasColumnName("descricao").IsRequired(false);
            e.Property(t => t.Prazo).HasColumnName("prazo");
            e.Property(t => t.EstimativaMin).HasColumnName("estimativa_min");
            e.Property(t => t.TempoRealMin).HasColumnName("tempo_real_min");
            // Enums gravados como texto (varchar), como no DER.
            e.Property(t => t.Criticidade).HasColumnName("criticidade").HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.Complexidade).HasColumnName("complexidade").HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.Prioridade).HasColumnName("prioridade").HasConversion<string>().HasMaxLength(30);
            e.Property(t => t.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.CriadoEm).HasColumnName("criada_em");
            e.Property(t => t.ConcluidoEm).HasColumnName("concluida_em");
            // US13 - lembrete antes do prazo (null = sem lembrete).
            e.Property(t => t.LembreteMin).HasColumnName("lembrete_min");
            e.Property(t => t.LembreteDispensadoEm).HasColumnName("lembrete_dispensado_em");

            // Listagens e filtros do usuário por status e por prazo.
            e.HasIndex(t => new { t.UsuarioId, t.Status });
            e.HasIndex(t => new { t.UsuarioId, t.Prazo });

            e.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            // A FK composta impede que pai e filha pertençam a usuários diferentes.
            e.HasOne(t => t.TarefaPai)
                .WithMany(t => t.Subtarefas)
                .HasForeignKey(t => new { t.TarefaPaiId, t.UsuarioId })
                .HasPrincipalKey(t => new { t.Id, t.UsuarioId })
                .OnDelete(DeleteBehavior.Restrict);
            e.Navigation(t => t.Subtarefas).UsePropertyAccessMode(PropertyAccessMode.Field);

            e.HasOne<Categoria>()
                .WithMany()
                .HasForeignKey(t => t.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            // US12 - os registros de histórico são apagados junto com a tarefa.
            e.HasMany(t => t.Historico)
                .WithOne()
                .HasForeignKey(h => h.TarefaId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(t => t.Historico).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<HistoricoStatus>(e =>
        {
            e.ToTable("historico_status", t =>
            {
                t.HasCheckConstraint("CK_historico_status_novo_valido",
                    "status_novo IN ('Pendente', 'EmAndamento', 'Concluida', 'Cancelada')");
                t.HasCheckConstraint("CK_historico_status_anterior_valido",
                    "status_anterior IS NULL OR status_anterior IN ('Pendente', 'EmAndamento', 'Concluida', 'Cancelada')");
            });
            e.HasKey(h => h.Id);
            e.Property(h => h.Id).HasColumnName("id_historico");
            e.Property(h => h.TarefaId).HasColumnName("id_tarefa");
            e.Property(h => h.StatusAnterior).HasColumnName("status_anterior").HasConversion<string>().HasMaxLength(20);
            e.Property(h => h.StatusNovo).HasColumnName("status_novo").HasConversion<string>().HasMaxLength(20);
            e.Property(h => h.AlteradoEm).HasColumnName("alterado_em");
            e.HasIndex(h => new { h.TarefaId, h.AlteradoEm });
        });

        modelBuilder.Entity<PlanejamentoItem>(e =>
        {
            e.ToTable("planejamento_item", t =>
            {
                t.HasCheckConstraint("CK_planejamento_horario_valido", "hora_fim > hora_inicio");
                t.HasCheckConstraint("CK_planejamento_ordem_nao_negativa", "ordem >= 0");
            });
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id_planejamento_item");
            e.Property(p => p.UsuarioId).HasColumnName("id_usuario");
            e.Property(p => p.SemanaInicio).HasColumnName("semana_inicio");
            e.Property(p => p.TarefaId).HasColumnName("id_tarefa");
            e.Property(p => p.DisponibilidadeId).HasColumnName("id_disponibilidade");
            e.Property(p => p.Data).HasColumnName("data");
            e.Property(p => p.HoraInicio).HasColumnName("hora_inicio");
            e.Property(p => p.HoraFim).HasColumnName("hora_fim");
            e.Property(p => p.Ordem).HasColumnName("ordem");

            // Uma tarefa aparece no máximo uma vez no planejamento da mesma semana.
            e.HasIndex(p => new { p.UsuarioId, p.SemanaInicio, p.TarefaId }).IsUnique();
            e.HasIndex(p => new { p.UsuarioId, p.SemanaInicio, p.DisponibilidadeId, p.Ordem });

            e.HasOne<Usuario>()
                .WithMany()
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne<Tarefa>()
                .WithMany()
                .HasForeignKey(p => p.TarefaId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne<Disponibilidade>()
                .WithMany()
                .HasForeignKey(p => p.DisponibilidadeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private class DataUtcConverter : ValueConverter<DateTime, DateTime>
    {
        public DataUtcConverter() : base(
            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        { }
    }

    private class DataUtcNulavelConverter : ValueConverter<DateTime?, DateTime?>
    {
        public DataUtcNulavelConverter() : base(
            v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
        { }
    }
}
