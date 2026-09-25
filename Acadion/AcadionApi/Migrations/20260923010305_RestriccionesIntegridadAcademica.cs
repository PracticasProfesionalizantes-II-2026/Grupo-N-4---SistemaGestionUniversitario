using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class RestriccionesIntegridadAcademica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EstudianteMaterias_IdEstudiante",
                table: "EstudianteMaterias");

            migrationBuilder.DropIndex(
                name: "IX_Asistencia_IdEstudianteMateria",
                table: "Asistencia");

            migrationBuilder.CreateIndex(
                name: "IX_EstudianteMaterias_IdEstudiante_IdMateria_CicloLectivo",
                table: "EstudianteMaterias",
                columns: new[] { "IdEstudiante", "IdMateria", "CicloLectivo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Asistencia_IdEstudianteMateria_Fecha",
                table: "Asistencia",
                columns: new[] { "IdEstudianteMateria", "Fecha" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EstudianteMaterias_IdEstudiante_IdMateria_CicloLectivo",
                table: "EstudianteMaterias");

            migrationBuilder.DropIndex(
                name: "IX_Asistencia_IdEstudianteMateria_Fecha",
                table: "Asistencia");

            migrationBuilder.CreateIndex(
                name: "IX_EstudianteMaterias_IdEstudiante",
                table: "EstudianteMaterias",
                column: "IdEstudiante");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencia_IdEstudianteMateria",
                table: "Asistencia",
                column: "IdEstudianteMateria");
        }
    }
}
