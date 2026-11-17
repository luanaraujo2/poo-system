using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GestaoTempo.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPlanejamentoSemanal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "planejamento_item",
                columns: table => new
                {
                    id_planejamento_item = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_usuario = table.Column<int>(type: "integer", nullable: false),
                    semana_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    id_tarefa = table.Column<int>(type: "integer", nullable: false),
                    id_disponibilidade = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fim = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planejamento_item", x => x.id_planejamento_item);
                    table.CheckConstraint("CK_planejamento_horario_valido", "hora_fim > hora_inicio");
                    table.CheckConstraint("CK_planejamento_ordem_nao_negativa", "ordem >= 0");
                    table.ForeignKey(
                        name: "FK_planejamento_item_disponibilidade_id_disponibilidade",
                        column: x => x.id_disponibilidade,
                        principalTable: "disponibilidade",
                        principalColumn: "id_disponibilidade",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_planejamento_item_tarefa_id_tarefa",
                        column: x => x.id_tarefa,
                        principalTable: "tarefa",
                        principalColumn: "id_tarefa",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_planejamento_item_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_planejamento_item_id_disponibilidade",
                table: "planejamento_item",
                column: "id_disponibilidade");

            migrationBuilder.CreateIndex(
                name: "IX_planejamento_item_id_tarefa",
                table: "planejamento_item",
                column: "id_tarefa");

            migrationBuilder.CreateIndex(
                name: "IX_planejamento_item_id_usuario_semana_inicio_id_disponibilida~",
                table: "planejamento_item",
                columns: new[] { "id_usuario", "semana_inicio", "id_disponibilidade", "ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_planejamento_item_id_usuario_semana_inicio_id_tarefa",
                table: "planejamento_item",
                columns: new[] { "id_usuario", "semana_inicio", "id_tarefa" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "planejamento_item");
        }
    }
}
