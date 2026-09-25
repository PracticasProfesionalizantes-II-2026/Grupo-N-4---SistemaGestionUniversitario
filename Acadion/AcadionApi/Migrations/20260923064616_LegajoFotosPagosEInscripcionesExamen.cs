using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class LegajoFotosPagosEInscripcionesExamen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Legajo",
                table: "Usuarios",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "FotoPerfilUrl",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CuotasMensuales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstudianteId = table.Column<int>(type: "int", nullable: false),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ComprobanteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuotasMensuales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuotasMensuales_Usuarios_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InscripcionesExamenes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamenId = table.Column<int>(type: "int", nullable: false),
                    EstudianteId = table.Column<int>(type: "int", nullable: false),
                    FechaInscripcionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InscripcionesExamenes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InscripcionesExamenes_Examen_ExamenId",
                        column: x => x.ExamenId,
                        principalTable: "Examen",
                        principalColumn: "IdExamen",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InscripcionesExamenes_Usuarios_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatriculasIniciales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstudianteId = table.Column<int>(type: "int", nullable: false),
                    PeriodoLectivo = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ComprobanteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatriculasIniciales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatriculasIniciales_Usuarios_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                UPDATE Usuarios
                SET Legajo = CONCAT('AC-', YEAR(GETUTCDATE()), '-', RIGHT('000000' + CAST(Id AS varchar(6)), 6))
                WHERE Rol = 1 AND (Legajo IS NULL OR LTRIM(RTRIM(Legajo)) = '');

                ;WITH LegajosDuplicados AS
                (
                    SELECT Id, ROW_NUMBER() OVER (PARTITION BY Legajo ORDER BY Id) AS Numero
                    FROM Usuarios
                    WHERE Legajo IS NOT NULL AND LTRIM(RTRIM(Legajo)) <> ''
                )
                UPDATE u
                SET Legajo = CONCAT('AC-', YEAR(GETUTCDATE()), '-', RIGHT('000000' + CAST(u.Id AS varchar(6)), 6))
                FROM Usuarios u
                INNER JOIN LegajosDuplicados d ON d.Id = u.Id
                WHERE d.Numero > 1;

                INSERT INTO MatriculasIniciales
                    (EstudianteId, PeriodoLectivo, Estado, FechaPago, ComprobanteUrl, FechaCreacionUtc)
                SELECT Id, YEAR(GETUTCDATE()), 'Pendiente', NULL, '', SYSUTCDATETIME()
                FROM Usuarios u
                WHERE u.Rol = 1
                  AND NOT EXISTS (SELECT 1 FROM MatriculasIniciales m WHERE m.EstudianteId = u.Id);

                INSERT INTO CuotasMensuales
                    (EstudianteId, Anio, Mes, Estado, FechaVencimiento, FechaPago, ComprobanteUrl, FechaCreacionUtc)
                SELECT Id, YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 'Pendiente',
                    DATEFROMPARTS(YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 10), NULL, '', SYSUTCDATETIME()
                FROM Usuarios u
                WHERE u.Rol = 1
                  AND NOT EXISTS (
                      SELECT 1 FROM CuotasMensuales c
                      WHERE c.EstudianteId = u.Id
                        AND c.Anio = YEAR(GETUTCDATE())
                        AND c.Mes = MONTH(GETUTCDATE()));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Legajo",
                table: "Usuarios",
                column: "Legajo",
                unique: true,
                filter: "[Legajo] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_CuotasMensuales_EstudianteId_Anio_Mes",
                table: "CuotasMensuales",
                columns: new[] { "EstudianteId", "Anio", "Mes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InscripcionesExamenes_EstudianteId",
                table: "InscripcionesExamenes",
                column: "EstudianteId");

            migrationBuilder.CreateIndex(
                name: "IX_InscripcionesExamenes_ExamenId_EstudianteId",
                table: "InscripcionesExamenes",
                columns: new[] { "ExamenId", "EstudianteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatriculasIniciales_EstudianteId_PeriodoLectivo",
                table: "MatriculasIniciales",
                columns: new[] { "EstudianteId", "PeriodoLectivo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CuotasMensuales");

            migrationBuilder.DropTable(
                name: "InscripcionesExamenes");

            migrationBuilder.DropTable(
                name: "MatriculasIniciales");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Legajo",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "FotoPerfilUrl",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "Legajo",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
