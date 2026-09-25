using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class DetalleYJustificacionInasistencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CantidadInasistencias",
                table: "Asistencia",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Justificada",
                table: "Asistencia",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TemaDictado",
                table: "Asistencia",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoClase",
                table: "Asistencia",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE a
                SET a.TipoClase = CASE
                        WHEN LOWER(ISNULL(m.Modalidad, '')) = 'virtual' THEN 'Virtual'
                        ELSE 'Presencial'
                    END,
                    a.TemaDictado = 'Sin tema registrado',
                    a.Justificada = CASE WHEN a.Tipo = 'Justificada' THEN 1 ELSE 0 END,
                    a.CantidadInasistencias = CASE
                        WHEN a.Tipo IN ('Ausente', 'Justificada') THEN 1
                        ELSE 0
                    END,
                    a.Tipo = CASE WHEN a.Tipo = 'Justificada' THEN 'Ausente' ELSE a.Tipo END
                FROM Asistencia a
                LEFT JOIN EstudianteMaterias em
                    ON em.IdEstudianteMateria = a.IdEstudianteMateria
                LEFT JOIN Materias m
                    ON m.IdMateria = em.IdMateria;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CantidadInasistencias",
                table: "Asistencia");

            migrationBuilder.DropColumn(
                name: "Justificada",
                table: "Asistencia");

            migrationBuilder.DropColumn(
                name: "TemaDictado",
                table: "Asistencia");

            migrationBuilder.DropColumn(
                name: "TipoClase",
                table: "Asistencia");
        }
    }
}
