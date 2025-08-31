using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstagioGO.Migrations
{
    /// <inheritdoc />
    public partial class JustificativaMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItensAvaliacao");

            migrationBuilder.DropColumn(
                name: "PeriodoAvaliacaoId",
                table: "Avaliacoes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PeriodoAvaliacaoId",
                table: "Avaliacoes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ItensAvaliacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AvaliacaoId = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nota = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensAvaliacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensAvaliacao_Avaliacoes_AvaliacaoId",
                        column: x => x.AvaliacaoId,
                        principalTable: "Avaliacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItensAvaliacao_AvaliacaoId",
                table: "ItensAvaliacao",
                column: "AvaliacaoId");
        }
    }
}
