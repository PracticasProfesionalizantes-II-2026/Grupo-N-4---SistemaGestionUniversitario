using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    public partial class GestionCarrerasYCorrelatividades : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Anio_Carrera_IdCarrera", table: "Anio");
            migrationBuilder.DropForeignKey(name: "FK_Usuarios_Carrera_CarreraIdCarrera", table: "Usuarios");

            migrationBuilder.RenameColumn(name: "CarreraIdCarrera", table: "Usuarios", newName: "CarreraId");
            migrationBuilder.RenameIndex(name: "IX_Usuarios_CarreraIdCarrera", table: "Usuarios", newName: "IX_Usuarios_CarreraId");

            migrationBuilder.AlterColumn<string>(name: "PlanEstudios", table: "Carrera", type: "nvarchar(80)", maxLength: 80, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AlterColumn<string>(name: "Nombre", table: "Carrera", type: "nvarchar(150)", maxLength: 150, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
            migrationBuilder.AddColumn<int>(name: "CapacidadMaximaEstudiantes", table: "Carrera", type: "int", nullable: false, defaultValue: 100);
            migrationBuilder.AddColumn<int>(name: "DuracionAnios", table: "Carrera", type: "int", nullable: false, defaultValue: 1);
            migrationBuilder.AddColumn<string>(name: "Tipo", table: "Carrera", type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Tecnicatura");

            migrationBuilder.Sql(@"
                UPDATE c
                SET DuracionAnios = CASE WHEN x.MaximoAnio IS NULL OR x.MaximoAnio < 1 THEN 1 ELSE x.MaximoAnio END,
                    CapacidadMaximaEstudiantes = CASE WHEN x.Estudiantes > 100 THEN x.Estudiantes ELSE 100 END
                FROM Carrera c
                OUTER APPLY (
                    SELECT MAX(a.NumeroAnio) AS MaximoAnio,
                           (SELECT COUNT(*) FROM Usuarios u WHERE u.CarreraId = c.IdCarrera) AS Estudiantes
                    FROM Anio a WHERE a.IdCarrera = c.IdCarrera
                ) x;");

            migrationBuilder.CreateTable(
                name: "MateriaCorrelativa",
                columns: table => new
                {
                    IdMateria = table.Column<int>(type: "int", nullable: false),
                    IdCorrelativa = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MateriaCorrelativa", x => new { x.IdMateria, x.IdCorrelativa });
                    table.ForeignKey(name: "FK_MateriaCorrelativa_Materias_IdCorrelativa", column: x => x.IdCorrelativa, principalTable: "Materias", principalColumn: "IdMateria", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_MateriaCorrelativa_Materias_IdMateria", column: x => x.IdMateria, principalTable: "Materias", principalColumn: "IdMateria", onDelete: ReferentialAction.Cascade);
                });

            // Conserva las correlatividades existentes al corregir la relación 1:N a N:N.
            migrationBuilder.Sql(@"
                INSERT INTO MateriaCorrelativa (IdMateria, IdCorrelativa)
                SELECT MateriaIdMateria, IdMateria
                FROM Materias
                WHERE MateriaIdMateria IS NOT NULL AND MateriaIdMateria <> IdMateria;");

            migrationBuilder.DropForeignKey(name: "FK_Materias_Materias_MateriaIdMateria", table: "Materias");
            migrationBuilder.DropIndex(name: "IX_Materias_MateriaIdMateria", table: "Materias");
            migrationBuilder.DropColumn(name: "MateriaIdMateria", table: "Materias");
            migrationBuilder.DropIndex(name: "IX_Anio_IdCarrera", table: "Anio");

            migrationBuilder.CreateIndex(name: "IX_Carrera_Nombre_PlanEstudios", table: "Carrera", columns: new[] { "Nombre", "PlanEstudios" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_Anio_IdCarrera_NumeroAnio", table: "Anio", columns: new[] { "IdCarrera", "NumeroAnio" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_MateriaCorrelativa_IdCorrelativa", table: "MateriaCorrelativa", column: "IdCorrelativa");

            migrationBuilder.AddForeignKey(name: "FK_Anio_Carrera_IdCarrera", table: "Anio", column: "IdCarrera", principalTable: "Carrera", principalColumn: "IdCarrera", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_Usuarios_Carrera_CarreraId", table: "Usuarios", column: "CarreraId", principalTable: "Carrera", principalColumn: "IdCarrera", onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Anio_Carrera_IdCarrera", table: "Anio");
            migrationBuilder.DropForeignKey(name: "FK_Usuarios_Carrera_CarreraId", table: "Usuarios");
            migrationBuilder.DropIndex(name: "IX_Carrera_Nombre_PlanEstudios", table: "Carrera");
            migrationBuilder.DropIndex(name: "IX_Anio_IdCarrera_NumeroAnio", table: "Anio");

            migrationBuilder.AddColumn<int>(name: "MateriaIdMateria", table: "Materias", type: "int", nullable: true);
            migrationBuilder.Sql(@"
                UPDATE m
                SET MateriaIdMateria = mc.IdMateria
                FROM Materias m
                INNER JOIN MateriaCorrelativa mc ON mc.IdCorrelativa = m.IdMateria;");
            migrationBuilder.DropTable(name: "MateriaCorrelativa");

            migrationBuilder.DropColumn(name: "CapacidadMaximaEstudiantes", table: "Carrera");
            migrationBuilder.DropColumn(name: "DuracionAnios", table: "Carrera");
            migrationBuilder.DropColumn(name: "Tipo", table: "Carrera");
            migrationBuilder.RenameColumn(name: "CarreraId", table: "Usuarios", newName: "CarreraIdCarrera");
            migrationBuilder.RenameIndex(name: "IX_Usuarios_CarreraId", table: "Usuarios", newName: "IX_Usuarios_CarreraIdCarrera");
            migrationBuilder.AlterColumn<string>(name: "PlanEstudios", table: "Carrera", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(80)", oldMaxLength: 80);
            migrationBuilder.AlterColumn<string>(name: "Nombre", table: "Carrera", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(150)", oldMaxLength: 150);

            migrationBuilder.CreateIndex(name: "IX_Materias_MateriaIdMateria", table: "Materias", column: "MateriaIdMateria");
            migrationBuilder.CreateIndex(name: "IX_Anio_IdCarrera", table: "Anio", column: "IdCarrera");
            migrationBuilder.AddForeignKey(name: "FK_Anio_Carrera_IdCarrera", table: "Anio", column: "IdCarrera", principalTable: "Carrera", principalColumn: "IdCarrera", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_Materias_Materias_MateriaIdMateria", table: "Materias", column: "MateriaIdMateria", principalTable: "Materias", principalColumn: "IdMateria");
            migrationBuilder.AddForeignKey(name: "FK_Usuarios_Carrera_CarreraIdCarrera", table: "Usuarios", column: "CarreraIdCarrera", principalTable: "Carrera", principalColumn: "IdCarrera");
        }
    }
}
