using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class RegistroClases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClaseId",
                table: "Asistencia",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClasesAcademicas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MateriaId = table.Column<int>(type: "int", nullable: false),
                    ComisionId = table.Column<int>(type: "int", nullable: true),
                    DocenteId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modalidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AulaOEnlace = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClasesAcademicas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClasesAcademicas_Comisiones_ComisionId",
                        column: x => x.ComisionId,
                        principalTable: "Comisiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClasesAcademicas_Materias_MateriaId",
                        column: x => x.MateriaId,
                        principalTable: "Materias",
                        principalColumn: "IdMateria",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClasesAcademicas_Usuarios_DocenteId",
                        column: x => x.DocenteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asistencia_ClaseId",
                table: "Asistencia",
                column: "ClaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ClasesAcademicas_ComisionId",
                table: "ClasesAcademicas",
                column: "ComisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClasesAcademicas_DocenteId",
                table: "ClasesAcademicas",
                column: "DocenteId");

            migrationBuilder.CreateIndex(
                name: "IX_ClasesAcademicas_MateriaId_ComisionId_Fecha",
                table: "ClasesAcademicas",
                columns: new[] { "MateriaId", "ComisionId", "Fecha" },
                unique: true,
                filter: "[ComisionId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Asistencia_ClasesAcademicas_ClaseId",
                table: "Asistencia",
                column: "ClaseId",
                principalTable: "ClasesAcademicas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asistencia_ClasesAcademicas_ClaseId",
                table: "Asistencia");

            migrationBuilder.DropTable(
                name: "ClasesAcademicas");

            migrationBuilder.DropIndex(
                name: "IX_Asistencia_ClaseId",
                table: "Asistencia");

            migrationBuilder.DropColumn(
                name: "ClaseId",
                table: "Asistencia");
        }
    }
}
