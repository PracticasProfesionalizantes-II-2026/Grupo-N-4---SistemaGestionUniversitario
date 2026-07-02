using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Carrera",
                columns: table => new
                {
                    IdCarrera = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlanEstudios = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carrera", x => x.IdCarrera);
                });

            migrationBuilder.CreateTable(
                name: "Personas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dni = table.Column<long>(type: "bigint", nullable: false),
                    FechaNacimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Localidad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodigoPostal = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Anio",
                columns: table => new
                {
                    IdAnio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroAnio = table.Column<int>(type: "int", nullable: false),
                    NombreAnio = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdCarrera = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anio", x => x.IdAnio);
                    table.ForeignKey(
                        name: "FK_Anio_Carrera_IdCarrera",
                        column: x => x.IdCarrera,
                        principalTable: "Carrera",
                        principalColumn: "IdCarrera",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonaId = table.Column<int>(type: "int", nullable: false),
                    NombreUsuario = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaUltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmailInstitucional = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TelefonoContacto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Matricula = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Legajo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PromedioGeneral = table.Column<double>(type: "float", nullable: false),
                    Especialidad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TituloAcademico = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CarreraIdCarrera = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Carrera_CarreraIdCarrera",
                        column: x => x.CarreraIdCarrera,
                        principalTable: "Carrera",
                        principalColumn: "IdCarrera");
                    table.ForeignKey(
                        name: "FK_Usuarios_Personas_PersonaId",
                        column: x => x.PersonaId,
                        principalTable: "Personas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Materias",
                columns: table => new
                {
                    IdMateria = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Modalidad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdAnio = table.Column<int>(type: "int", nullable: false),
                    MateriaIdMateria = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materias", x => x.IdMateria);
                    table.ForeignKey(
                        name: "FK_Materias_Anio_IdAnio",
                        column: x => x.IdAnio,
                        principalTable: "Anio",
                        principalColumn: "IdAnio",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Materias_Materias_MateriaIdMateria",
                        column: x => x.MateriaIdMateria,
                        principalTable: "Materias",
                        principalColumn: "IdMateria");
                });

            migrationBuilder.CreateTable(
                name: "EstudianteMaterias",
                columns: table => new
                {
                    IdEstudianteMateria = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEstudiante = table.Column<int>(type: "int", nullable: false),
                    IdMateria = table.Column<int>(type: "int", nullable: false),
                    IdDocente = table.Column<int>(type: "int", nullable: false),
                    CicloLectivo = table.Column<int>(type: "int", nullable: false),
                    Cuatrimestre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaInscripcion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstudianteMaterias", x => x.IdEstudianteMateria);
                    table.ForeignKey(
                        name: "FK_EstudianteMaterias_Materias_IdMateria",
                        column: x => x.IdMateria,
                        principalTable: "Materias",
                        principalColumn: "IdMateria",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstudianteMaterias_Usuarios_IdDocente",
                        column: x => x.IdDocente,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstudianteMaterias_Usuarios_IdEstudiante",
                        column: x => x.IdEstudiante,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Examen",
                columns: table => new
                {
                    IdExamen = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdMateria = table.Column<int>(type: "int", nullable: false),
                    CicloLectivo = table.Column<int>(type: "int", nullable: false),
                    IdDocente = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TipoExamen = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Examen", x => x.IdExamen);
                    table.ForeignKey(
                        name: "FK_Examen_Materias_IdMateria",
                        column: x => x.IdMateria,
                        principalTable: "Materias",
                        principalColumn: "IdMateria",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Examen_Usuarios_IdDocente",
                        column: x => x.IdDocente,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HorarioMateria",
                columns: table => new
                {
                    IdHorarioMateria = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdMateria = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HoraInicio = table.Column<TimeSpan>(type: "time", nullable: false),
                    HoraFin = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorarioMateria", x => x.IdHorarioMateria);
                    table.ForeignKey(
                        name: "FK_HorarioMateria_Materias_IdMateria",
                        column: x => x.IdMateria,
                        principalTable: "Materias",
                        principalColumn: "IdMateria",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Asistencia",
                columns: table => new
                {
                    IdAsistencia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEstudianteMateria = table.Column<int>(type: "int", nullable: false),
                    IdDocente = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asistencia", x => x.IdAsistencia);
                    table.ForeignKey(
                        name: "FK_Asistencia_EstudianteMaterias_IdEstudianteMateria",
                        column: x => x.IdEstudianteMateria,
                        principalTable: "EstudianteMaterias",
                        principalColumn: "IdEstudianteMateria",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Asistencia_Usuarios_IdDocente",
                        column: x => x.IdDocente,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotaExamen",
                columns: table => new
                {
                    IdNota = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdExamen = table.Column<int>(type: "int", nullable: false),
                    IdEstudiante = table.Column<int>(type: "int", nullable: false),
                    Nota = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotaExamen", x => x.IdNota);
                    table.ForeignKey(
                        name: "FK_NotaExamen_Examen_IdExamen",
                        column: x => x.IdExamen,
                        principalTable: "Examen",
                        principalColumn: "IdExamen",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotaExamen_Usuarios_IdEstudiante",
                        column: x => x.IdEstudiante,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Anio_IdCarrera",
                table: "Anio",
                column: "IdCarrera");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencia_IdDocente",
                table: "Asistencia",
                column: "IdDocente");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencia_IdEstudianteMateria",
                table: "Asistencia",
                column: "IdEstudianteMateria");

            migrationBuilder.CreateIndex(
                name: "IX_EstudianteMaterias_IdDocente",
                table: "EstudianteMaterias",
                column: "IdDocente");

            migrationBuilder.CreateIndex(
                name: "IX_EstudianteMaterias_IdEstudiante",
                table: "EstudianteMaterias",
                column: "IdEstudiante");

            migrationBuilder.CreateIndex(
                name: "IX_EstudianteMaterias_IdMateria",
                table: "EstudianteMaterias",
                column: "IdMateria");

            migrationBuilder.CreateIndex(
                name: "IX_Examen_IdDocente",
                table: "Examen",
                column: "IdDocente");

            migrationBuilder.CreateIndex(
                name: "IX_Examen_IdMateria",
                table: "Examen",
                column: "IdMateria");

            migrationBuilder.CreateIndex(
                name: "IX_HorarioMateria_IdMateria",
                table: "HorarioMateria",
                column: "IdMateria");

            migrationBuilder.CreateIndex(
                name: "IX_Materias_IdAnio",
                table: "Materias",
                column: "IdAnio");

            migrationBuilder.CreateIndex(
                name: "IX_Materias_MateriaIdMateria",
                table: "Materias",
                column: "MateriaIdMateria");

            migrationBuilder.CreateIndex(
                name: "IX_NotaExamen_IdEstudiante",
                table: "NotaExamen",
                column: "IdEstudiante");

            migrationBuilder.CreateIndex(
                name: "IX_NotaExamen_IdExamen",
                table: "NotaExamen",
                column: "IdExamen");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CarreraIdCarrera",
                table: "Usuarios",
                column: "CarreraIdCarrera");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_PersonaId",
                table: "Usuarios",
                column: "PersonaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Asistencia");

            migrationBuilder.DropTable(
                name: "HorarioMateria");

            migrationBuilder.DropTable(
                name: "NotaExamen");

            migrationBuilder.DropTable(
                name: "EstudianteMaterias");

            migrationBuilder.DropTable(
                name: "Examen");

            migrationBuilder.DropTable(
                name: "Materias");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Anio");

            migrationBuilder.DropTable(
                name: "Personas");

            migrationBuilder.DropTable(
                name: "Carrera");
        }
    }
}
