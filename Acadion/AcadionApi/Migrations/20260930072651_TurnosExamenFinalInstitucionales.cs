using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class TurnosExamenFinalInstitucionales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TurnoExamenFinalId",
                table: "Examen",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TurnosExamenFinal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CicloLectivo = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    NumeroLlamado = table.Column<int>(type: "int", nullable: false),
                    FechaInicioUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFinUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurnosExamenFinal", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Examen_TurnoExamenFinalId",
                table: "Examen",
                column: "TurnoExamenFinalId");

            migrationBuilder.CreateIndex(
                name: "IX_TurnosExamenFinal_CicloLectivo_Nombre_NumeroLlamado",
                table: "TurnosExamenFinal",
                columns: new[] { "CicloLectivo", "Nombre", "NumeroLlamado" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Examen_TurnosExamenFinal_TurnoExamenFinalId",
                table: "Examen",
                column: "TurnoExamenFinalId",
                principalTable: "TurnosExamenFinal",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Examen_TurnosExamenFinal_TurnoExamenFinalId",
                table: "Examen");

            migrationBuilder.DropTable(name: "TurnosExamenFinal");

            migrationBuilder.DropIndex(
                name: "IX_Examen_TurnoExamenFinalId",
                table: "Examen");

            migrationBuilder.DropColumn(
                name: "TurnoExamenFinalId",
                table: "Examen");
        }
    }
}
