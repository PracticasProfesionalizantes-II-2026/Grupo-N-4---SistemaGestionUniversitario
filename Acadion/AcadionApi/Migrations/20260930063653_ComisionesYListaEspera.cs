using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class ComisionesYListaEspera : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ComisionId",
                table: "EstudianteMaterias",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Comisiones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MateriaId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Turno = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CicloLectivo = table.Column<int>(type: "int", nullable: false),
                    Cupo = table.Column<int>(type: "int", nullable: false),
                    DocenteId = table.Column<int>(type: "int", nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comisiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comisiones_Materias_MateriaId",
                        column: x => x.MateriaId,
                        principalTable: "Materias",
                        principalColumn: "IdMateria",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comisiones_Usuarios_DocenteId",
                        column: x => x.DocenteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HorariosComisiones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComisionId = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosComisiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorariosComisiones_Comisiones_ComisionId",
                        column: x => x.ComisionId,
                        principalTable: "Comisiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListasEsperaComisiones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComisionId = table.Column<int>(type: "int", nullable: false),
                    EstudianteId = table.Column<int>(type: "int", nullable: false),
                    FechaSolicitudUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaResolucionUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResueltoPorUsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListasEsperaComisiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListasEsperaComisiones_Comisiones_ComisionId",
                        column: x => x.ComisionId,
                        principalTable: "Comisiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListasEsperaComisiones_Usuarios_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ListasEsperaComisiones_Usuarios_ResueltoPorUsuarioId",
                        column: x => x.ResueltoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EstudianteMaterias_ComisionId",
                table: "EstudianteMaterias",
                column: "ComisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Comisiones_DocenteId",
                table: "Comisiones",
                column: "DocenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Comisiones_MateriaId_CicloLectivo_Nombre",
                table: "Comisiones",
                columns: new[] { "MateriaId", "CicloLectivo", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HorariosComisiones_ComisionId",
                table: "HorariosComisiones",
                column: "ComisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ListasEsperaComisiones_ComisionId_EstudianteId",
                table: "ListasEsperaComisiones",
                columns: new[] { "ComisionId", "EstudianteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListasEsperaComisiones_EstudianteId",
                table: "ListasEsperaComisiones",
                column: "EstudianteId");

            migrationBuilder.CreateIndex(
                name: "IX_ListasEsperaComisiones_ResueltoPorUsuarioId",
                table: "ListasEsperaComisiones",
                column: "ResueltoPorUsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_EstudianteMaterias_Comisiones_ComisionId",
                table: "EstudianteMaterias",
                column: "ComisionId",
                principalTable: "Comisiones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EstudianteMaterias_Comisiones_ComisionId",
                table: "EstudianteMaterias");

            migrationBuilder.DropTable(
                name: "HorariosComisiones");

            migrationBuilder.DropTable(
                name: "ListasEsperaComisiones");

            migrationBuilder.DropTable(
                name: "Comisiones");

            migrationBuilder.DropIndex(
                name: "IX_EstudianteMaterias_ComisionId",
                table: "EstudianteMaterias");

            migrationBuilder.DropColumn(
                name: "ComisionId",
                table: "EstudianteMaterias");
        }
    }
}
