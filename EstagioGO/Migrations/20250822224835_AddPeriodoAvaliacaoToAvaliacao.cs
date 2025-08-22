using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstagioGO.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodoAvaliacaoToAvaliacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Observacao",
                table: "Frequencias",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Comentarios",
                table: "Avaliacoes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "PeriodoAvaliacaoId",
                table: "Avaliacoes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PeriodoAvaliacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFim = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
                name: "PeriodoAvaliacaoId",
                table: "Avaliacoes");

            migrationBuilder.AlterColumn<string>(
                name: "Observacao",
                table: "Frequencias",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Comentarios",
                table: "Avaliacoes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);
        }
    }
}
