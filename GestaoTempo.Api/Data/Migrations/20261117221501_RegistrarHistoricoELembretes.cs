using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GestaoTempo.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RegistrarHistoricoELembretes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "lembrete_dispensado_em",
                table: "tarefa",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "lembrete_min",
                table: "tarefa",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "historico_status",
                columns: table => new
                {
                    id_historico = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_tarefa = table.Column<int>(type: "integer", nullable: false),
                    status_anterior = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status_novo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    alterado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historico_status", x => x.id_historico);
                    table.CheckConstraint("CK_historico_status_anterior_valido", "status_anterior IS NULL OR status_anterior IN ('Pendente', 'EmAndamento', 'Concluida', 'Cancelada')");
                    table.CheckConstraint("CK_historico_status_novo_valido", "status_novo IN ('Pendente', 'EmAndamento', 'Concluida', 'Cancelada')");
                    table.ForeignKey(
                        name: "FK_historico_status_tarefa_id_tarefa",
                        column: x => x.id_tarefa,
                        principalTable: "tarefa",
                        principalColumn: "id_tarefa",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_tarefa_lembrete_valido",
                table: "tarefa",
                sql: "lembrete_min IS NULL OR lembrete_min BETWEEN 1 AND 43200");

            migrationBuilder.CreateIndex(
                name: "IX_historico_status_id_tarefa_alterado_em",
                table: "historico_status",
                columns: new[] { "id_tarefa", "alterado_em" });

            // Tarefas que já existiam: lembrete padrão de 24 horas (US13) e histórico inicial (US12),
            // com o cadastro e, se o status já mudou, a mudança para o status atual.
            migrationBuilder.Sql("UPDATE tarefa SET lembrete_min = 1440;");
            migrationBuilder.Sql(@"
                INSERT INTO historico_status (id_tarefa, status_anterior, status_novo, alterado_em)
                SELECT id_tarefa, NULL, 'Pendente', criada_em FROM tarefa;");
            migrationBuilder.Sql(@"
                INSERT INTO historico_status (id_tarefa, status_anterior, status_novo, alterado_em)
                SELECT id_tarefa, 'Pendente', status, COALESCE(concluida_em, criada_em)
                FROM tarefa WHERE status <> 'Pendente';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historico_status");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tarefa_lembrete_valido",
                table: "tarefa");

            migrationBuilder.DropColumn(
                name: "lembrete_dispensado_em",
                table: "tarefa");

            migrationBuilder.DropColumn(
                name: "lembrete_min",
                table: "tarefa");
        }
    }
}
