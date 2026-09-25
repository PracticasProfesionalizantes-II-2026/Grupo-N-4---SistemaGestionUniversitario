using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class PeriodosInscripcionPorMateria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PeriodosInscripcionMaterias_CarreraId_CicloLectivo",
                table: "PeriodosInscripcionMaterias");

            migrationBuilder.AddColumn<int>(
                name: "MateriaId",
                table: "PeriodosInscripcionMaterias",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosInscripcionMaterias_CarreraId_CicloLectivo",
                table: "PeriodosInscripcionMaterias",
                columns: new[] { "CarreraId", "CicloLectivo" },
                unique: true,
                filter: "[MateriaId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosInscripcionMaterias_MateriaId_CicloLectivo",
                table: "PeriodosInscripcionMaterias",
                columns: new[] { "MateriaId", "CicloLectivo" },
                unique: true,
                filter: "[MateriaId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PeriodosInscripcionMaterias_Materias_MateriaId",
                table: "PeriodosInscripcionMaterias",
                column: "MateriaId",
                principalTable: "Materias",
                principalColumn: "IdMateria",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PeriodosInscripcionMaterias_Materias_MateriaId",
                table: "PeriodosInscripcionMaterias");

            migrationBuilder.DropIndex(
                name: "IX_PeriodosInscripcionMaterias_CarreraId_CicloLectivo",
                table: "PeriodosInscripcionMaterias");

            migrationBuilder.DropIndex(
                name: "IX_PeriodosInscripcionMaterias_MateriaId_CicloLectivo",
                table: "PeriodosInscripcionMaterias");

            migrationBuilder.DropColumn(
                name: "MateriaId",
                table: "PeriodosInscripcionMaterias");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosInscripcionMaterias_CarreraId_CicloLectivo",
                table: "PeriodosInscripcionMaterias",
                columns: new[] { "CarreraId", "CicloLectivo" },
                unique: true);
        }
    }
}
