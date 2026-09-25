using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class TipoCursadaMateria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumeroPeriodo",
                table: "Materias",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoCursada",
                table: "Materias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Anual");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumeroPeriodo",
                table: "Materias");

            migrationBuilder.DropColumn(
                name: "TipoCursada",
                table: "Materias");
        }
    }
}
