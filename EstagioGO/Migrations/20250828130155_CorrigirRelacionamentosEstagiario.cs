using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstagioGO.Migrations
{
    /// <inheritdoc />
    public partial class CorrigirRelacionamentosEstagiario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Avaliacoes_PeriodoAvaliacao_PeriodoAvaliacaoId",
                table: "Avaliacoes");

            migrationBuilder.DropTable(
                name: "PeriodoAvaliacao");

            migrationBuilder.DropIndex(
                name: "IX_Avaliacoes_PeriodoAvaliacaoId",
                table: "Avaliacoes");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "Justificativas");

            migrationBuilder.DropColumn(
                name: "RequerDocumentacao",
                table: "Justificativas");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataRegistro",
                table: "Justificativas",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Detalhamento",
                table: "Justificativas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Motivo",
                table: "Justificativas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UsuarioRegistroId",
                table: "Justificativas",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Justificativas_UsuarioRegistroId",
                table: "Justificativas",
                column: "UsuarioRegistroId");

            migrationBuilder.AddForeignKey(
                name: "FK_Justificativas_AspNetUsers_UsuarioRegistroId",
                table: "Justificativas",
                column: "UsuarioRegistroId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Justificativas_AspNetUsers_UsuarioRegistroId",
                table: "Justificativas");

            migrationBuilder.DropIndex(
                name: "IX_Justificativas_UsuarioRegistroId",
                table: "Justificativas");

            migrationBuilder.DropColumn(
                name: "DataRegistro",
                table: "Justificativas");

            migrationBuilder.DropColumn(
                name: "Detalhamento",
                table: "Justificativas");

            migrationBuilder.DropColumn(
                name: "Motivo",
                table: "Justificativas");

            migrationBuilder.DropColumn(
                name: "UsuarioRegistroId",
                table: "Justificativas");

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "Justificativas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequerDocumentacao",
                table: "Justificativas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PeriodoAvaliacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    DataFim = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodoAvaliacao", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacoes_PeriodoAvaliacaoId",
                table: "Avaliacoes",
                column: "PeriodoAvaliacaoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Avaliacoes_PeriodoAvaliacao_PeriodoAvaliacaoId",
                table: "Avaliacoes",
                column: "PeriodoAvaliacaoId",
                principalTable: "PeriodoAvaliacao",
                principalColumn: "Id");
        }
    }
}
