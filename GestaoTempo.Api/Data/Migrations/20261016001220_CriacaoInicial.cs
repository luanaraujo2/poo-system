using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GestaoTempo.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id_usuario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "citext", maxLength: 150, nullable: false),
                    senha_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.id_usuario);
                });

            migrationBuilder.CreateTable(
                name: "categoria",
                columns: table => new
                {
                    id_categoria = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_usuario = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    padrao = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categoria", x => x.id_categoria);
                    table.ForeignKey(
                        name: "FK_categoria_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tarefa",
                columns: table => new
                {
                    id_tarefa = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_usuario = table.Column<int>(type: "integer", nullable: false),
                    id_categoria = table.Column<int>(type: "integer", nullable: false),
                    titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    prazo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    estimativa_min = table.Column<int>(type: "integer", nullable: false),
                    tempo_real_min = table.Column<int>(type: "integer", nullable: false),
                    criticidade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    complexidade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    prioridade = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    criada_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    concluida_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tarefa", x => x.id_tarefa);
                    table.CheckConstraint("CK_tarefa_complexidade_valida", "complexidade IN ('Baixa', 'Media', 'Alta')");
                    table.CheckConstraint("CK_tarefa_criticidade_valida", "criticidade IN ('Baixa', 'Media', 'Alta', 'Critica')");
                    table.CheckConstraint("CK_tarefa_estimativa_positiva", "estimativa_min > 0");
                    table.CheckConstraint("CK_tarefa_prioridade_valida", "prioridade IN ('Urgente_Importante', 'Importante_NaoUrgente', 'Urgente_NaoImportante', 'NaoUrgente_NaoImportante')");
                    table.CheckConstraint("CK_tarefa_status_valido", "status IN ('Pendente', 'EmAndamento', 'Concluida', 'Cancelada')");
                    table.CheckConstraint("CK_tarefa_tempo_real_nao_negativo", "tempo_real_min >= 0");
                    table.CheckConstraint("CK_tarefa_titulo_preenchido", "char_length(btrim(titulo)) > 0");
                    table.ForeignKey(
                        name: "FK_tarefa_categoria_id_categoria",
                        column: x => x.id_categoria,
                        principalTable: "categoria",
                        principalColumn: "id_categoria",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tarefa_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuario",
                        principalColumn: "id_usuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_categoria_id_usuario_nome",
                table: "categoria",
                columns: new[] { "id_usuario", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tarefa_id_categoria",
                table: "tarefa",
                column: "id_categoria");

            migrationBuilder.CreateIndex(
                name: "IX_tarefa_id_usuario_prazo",
                table: "tarefa",
                columns: new[] { "id_usuario", "prazo" });

            migrationBuilder.CreateIndex(
                name: "IX_tarefa_id_usuario_status",
                table: "tarefa",
                columns: new[] { "id_usuario", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_usuario_email",
                table: "usuario",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tarefa");

            migrationBuilder.DropTable(
                name: "categoria");

            migrationBuilder.DropTable(
                name: "usuario");
        }
    }
}
