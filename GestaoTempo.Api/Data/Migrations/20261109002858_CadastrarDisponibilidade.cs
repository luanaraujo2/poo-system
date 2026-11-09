using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GestaoTempo.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CadastrarDisponibilidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "disponibilidade",
                columns: table => new
                {
                    id_disponibilidade = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_usuario = table.Column<int>(type: "integer", nullable: false),
                    dia_semana = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fim = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disponibilidade", x => x.id_disponibilidade);
                    table.CheckConstraint("CK_disponibilidade_dia_valido", "dia_semana IN ('Segunda', 'Terca', 'Quarta', 'Quinta', 'Sexta', 'Sabado', 'Domingo')");
                    table.CheckConstraint("CK_disponibilidade_horario_valido", "hora_fim > hora_inicio");
                    table.ForeignKey(
                        name: "FK_disponibilidade_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_disponibilidade_id_usuario_dia_semana_hora_inicio",
                table: "disponibilidade",
                columns: new[] { "id_usuario", "dia_semana", "hora_inicio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "disponibilidade");
        }
    }
}
