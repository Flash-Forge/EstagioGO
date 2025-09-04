using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstagioGO.Migrations
{
    /// <inheritdoc />
    public partial class CorrectFrequencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Frequencias_Justificativas_JustificativaId",
                table: "Frequencias");

            migrationBuilder.DropTable(
                name: "Justificativas");

            migrationBuilder.DropIndex(
                name: "IX_Frequencias_JustificativaId",
                table: "Frequencias");

            migrationBuilder.DropColumn(
                name: "JustificativaId",
                table: "Frequencias");

            migrationBuilder.AddColumn<string>(
                name: "Detalhamento",
                table: "Frequencias",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Motivo",
                table: "Frequencias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Detalhamento",
                table: "Frequencias");

            migrationBuilder.DropColumn(
                name: "Motivo",
                table: "Frequencias");

            migrationBuilder.AddColumn<int>(
                name: "JustificativaId",
                table: "Frequencias",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Justificativas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioRegistroId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    DataRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Detalhamento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Justificativas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Justificativas_AspNetUsers_UsuarioRegistroId",
                        column: x => x.UsuarioRegistroId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Frequencias_JustificativaId",
                table: "Frequencias",
                column: "JustificativaId");

            migrationBuilder.CreateIndex(
                name: "IX_Justificativas_UsuarioRegistroId",
                table: "Justificativas",
                column: "UsuarioRegistroId");

            migrationBuilder.AddForeignKey(
                name: "FK_Frequencias_Justificativas_JustificativaId",
                table: "Frequencias",
                column: "JustificativaId",
                principalTable: "Justificativas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
