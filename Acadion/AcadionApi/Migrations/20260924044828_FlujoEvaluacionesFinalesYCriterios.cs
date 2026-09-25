using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class FlujoEvaluacionesFinalesYCriterios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Condicion",
                table: "NotaExamen",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Desaprobó");

            migrationBuilder.AddColumn<decimal>(
                name: "NotaMinimaPromocion",
                table: "Examen",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NotaMinimaRegularizacion",
                table: "Examen",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                defaultValue: 6m);

            migrationBuilder.Sql("""
                UPDATE NotaExamen
                SET Condicion = CASE WHEN Nota >= 6 THEN N'Regularizó' ELSE N'Desaprobó' END
                """);

            migrationBuilder.CreateTable(
                name: "PeriodosInscripcionExamenes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CicloLectivo = table.Column<int>(type: "int", nullable: false),
                    FechaInicioUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFinUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModificadoPorUsuarioId = table.Column<int>(type: "int", nullable: false),
                    FechaModificacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodosInscripcionExamenes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeriodosInscripcionExamenes_Usuarios_ModificadoPorUsuarioId",
                        column: x => x.ModificadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosInscripcionExamenes_CicloLectivo",
                table: "PeriodosInscripcionExamenes",
                column: "CicloLectivo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosInscripcionExamenes_ModificadoPorUsuarioId",
                table: "PeriodosInscripcionExamenes",
                column: "ModificadoPorUsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PeriodosInscripcionExamenes");

            migrationBuilder.DropColumn(
                name: "Condicion",
                table: "NotaExamen");

            migrationBuilder.DropColumn(
                name: "NotaMinimaPromocion",
                table: "Examen");

            migrationBuilder.DropColumn(
                name: "NotaMinimaRegularizacion",
                table: "Examen");
        }
    }
}
