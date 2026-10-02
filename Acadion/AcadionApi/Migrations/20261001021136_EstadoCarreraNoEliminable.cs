using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class EstadoCarreraNoEliminable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activa",
                table: "Carrera",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activa",
                table: "Carrera");
        }
    }
}
