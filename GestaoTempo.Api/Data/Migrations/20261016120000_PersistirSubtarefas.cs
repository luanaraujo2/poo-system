using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestaoTempo.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PersistirSubtarefas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NULL deixa todas as tarefas que já existem como tarefas-raiz.
            migrationBuilder.AddColumn<int>(
                name: "id_tarefa_pai",
                table: "tarefa",
                type: "integer",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_tarefa_id_tarefa_id_usuario",
                table: "tarefa",
                columns: new[] { "id_tarefa", "id_usuario" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_tarefa_nao_pode_ser_pai_de_si_mesma",
                table: "tarefa",
                sql: "id_tarefa_pai IS NULL OR id_tarefa_pai <> id_tarefa");

            migrationBuilder.CreateIndex(
                name: "IX_tarefa_id_tarefa_pai_id_usuario",
                table: "tarefa",
                columns: new[] { "id_tarefa_pai", "id_usuario" });

            migrationBuilder.AddForeignKey(
                name: "FK_tarefa_tarefa_id_tarefa_pai_id_usuario",
                table: "tarefa",
                columns: new[] { "id_tarefa_pai", "id_usuario" },
                principalTable: "tarefa",
                principalColumns: new[] { "id_tarefa", "id_usuario" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tarefa_tarefa_id_tarefa_pai_id_usuario",
                table: "tarefa");

            migrationBuilder.DropIndex(
                name: "IX_tarefa_id_tarefa_pai_id_usuario",
                table: "tarefa");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tarefa_nao_pode_ser_pai_de_si_mesma",
                table: "tarefa");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_tarefa_id_tarefa_id_usuario",
                table: "tarefa");

            migrationBuilder.DropColumn(
                name: "id_tarefa_pai",
                table: "tarefa");
        }
    }
}