using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class CalendarioAcademicoCentral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventosCalendario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FechaInicioUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFinUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CarreraId = table.Column<int>(type: "int", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosCalendario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosCalendario_Carrera_CarreraId",
                        column: x => x.CarreraId,
                        principalTable: "Carrera",
                        principalColumn: "IdCarrera",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventosCalendario_Usuarios_CreadoPorUsuarioId",
                        column: x => x.CreadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosCalendario_CarreraId",
                table: "EventosCalendario",
                column: "CarreraId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosCalendario_CreadoPorUsuarioId",
                table: "EventosCalendario",
                column: "CreadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosCalendario_FechaInicioUtc_FechaFinUtc",
                table: "EventosCalendario",
                columns: new[] { "FechaInicioUtc", "FechaFinUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosCalendario");
        }
    }
}
