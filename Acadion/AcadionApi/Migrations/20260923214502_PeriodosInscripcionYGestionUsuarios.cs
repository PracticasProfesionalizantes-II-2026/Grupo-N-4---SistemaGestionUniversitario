using System;
using AcadionApi.Datos;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260923214502_PeriodosInscripcionYGestionUsuarios")]
    public partial class PeriodosInscripcionYGestionUsuarios : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "IdDocente",
                table: "EstudianteMaterias",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "PeriodosInscripcionMaterias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarreraId = table.Column<int>(type: "int", nullable: false),
                    CicloLectivo = table.Column<int>(type: "int", nullable: false),
                    FechaInicioUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFinUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModificadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    FechaModificacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodosInscripcionMaterias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeriodosInscripcionMaterias_Carrera_CarreraId",
                        column: x => x.CarreraId,
                        principalTable: "Carrera",
                        principalColumn: "IdCarrera",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PeriodosInscripcionMaterias_Usuarios_ModificadoPorUsuarioId",
                        column: x => x.ModificadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosInscripcionMaterias_CarreraId_CicloLectivo",
                table: "PeriodosInscripcionMaterias",
                columns: new[] { "CarreraId", "CicloLectivo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosInscripcionMaterias_ModificadoPorUsuarioId",
                table: "PeriodosInscripcionMaterias",
                column: "ModificadoPorUsuarioId");

            migrationBuilder.Sql("""
                DECLARE @Inicio datetime2 = CONVERT(date, SYSUTCDATETIME());
                INSERT INTO PeriodosInscripcionMaterias
                    (CarreraId, CicloLectivo, FechaInicioUtc, FechaFinUtc, ModificadoPorUsuarioId, FechaModificacionUtc)
                SELECT c.IdCarrera, YEAR(SYSUTCDATETIME()), @Inicio,
                       DATEADD(second, -1, DATEADD(day, 14, @Inicio)), NULL, SYSUTCDATETIME()
                FROM Carrera c;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PeriodosInscripcionMaterias");

            migrationBuilder.AlterColumn<int>(
                name: "IdDocente",
                table: "EstudianteMaterias",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
