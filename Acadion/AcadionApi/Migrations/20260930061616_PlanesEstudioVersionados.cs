using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class PlanesEstudioVersionados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlanEstudioId",
                table: "Usuarios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlanEstudioId",
                table: "Materias",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlanesEstudio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarreraId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    VigenteDesde = table.Column<int>(type: "int", nullable: false),
                    VigenteHasta = table.Column<int>(type: "int", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanesEstudio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanesEstudio_Carrera_CarreraId",
                        column: x => x.CarreraId,
                        principalTable: "Carrera",
                        principalColumn: "IdCarrera",
                        onDelete: ReferentialAction.Restrict);
                });

            // Conserva los datos existentes: cada carrera recibe una primera
            // versión basada en su campo histórico PlanEstudios y sus alumnos y
            // materias quedan ligados a esa versión.
            migrationBuilder.Sql("""
                INSERT INTO [PlanesEstudio]
                    ([CarreraId], [Codigo], [VigenteDesde], [VigenteHasta], [Activo], [FechaCreacionUtc])
                SELECT [IdCarrera],
                       CASE WHEN LTRIM(RTRIM([PlanEstudios])) = ''
                            THEN CONCAT('Plan ', YEAR(SYSUTCDATETIME()))
                            ELSE [PlanEstudios] END,
                       COALESCE(TRY_CONVERT(int, RIGHT(LTRIM(RTRIM([PlanEstudios])), 4)),
                                YEAR(SYSUTCDATETIME())),
                       NULL, 1, SYSUTCDATETIME()
                FROM [Carrera];

                UPDATE m
                   SET m.[PlanEstudioId] = p.[Id]
                FROM [Materias] m
                INNER JOIN [Anio] a ON a.[IdAnio] = m.[IdAnio]
                INNER JOIN [PlanesEstudio] p ON p.[CarreraId] = a.[IdCarrera] AND p.[Activo] = 1;

                UPDATE u
                   SET u.[PlanEstudioId] = p.[Id]
                FROM [Usuarios] u
                INNER JOIN [PlanesEstudio] p ON p.[CarreraId] = u.[CarreraId] AND p.[Activo] = 1
                WHERE u.[CarreraId] IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_PlanEstudioId",
                table: "Usuarios",
                column: "PlanEstudioId");

            migrationBuilder.CreateIndex(
                name: "IX_Materias_PlanEstudioId",
                table: "Materias",
                column: "PlanEstudioId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanesEstudio_CarreraId_Activo",
                table: "PlanesEstudio",
                columns: new[] { "CarreraId", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanesEstudio_CarreraId_Codigo",
                table: "PlanesEstudio",
                columns: new[] { "CarreraId", "Codigo" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Materias_PlanesEstudio_PlanEstudioId",
                table: "Materias",
                column: "PlanEstudioId",
                principalTable: "PlanesEstudio",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_PlanesEstudio_PlanEstudioId",
                table: "Usuarios",
                column: "PlanEstudioId",
                principalTable: "PlanesEstudio",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materias_PlanesEstudio_PlanEstudioId",
                table: "Materias");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_PlanesEstudio_PlanEstudioId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "PlanesEstudio");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_PlanEstudioId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Materias_PlanEstudioId",
                table: "Materias");

            migrationBuilder.DropColumn(
                name: "PlanEstudioId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "PlanEstudioId",
                table: "Materias");
        }
    }
}
