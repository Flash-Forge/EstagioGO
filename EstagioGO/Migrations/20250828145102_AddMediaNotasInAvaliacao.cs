using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EstagioGO.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaNotasInAvaliacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MediaNotas",
                table: "Avaliacoes",
                type: "decimal(3,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaNotas",
                table: "Avaliacoes");
        }
    }
}
