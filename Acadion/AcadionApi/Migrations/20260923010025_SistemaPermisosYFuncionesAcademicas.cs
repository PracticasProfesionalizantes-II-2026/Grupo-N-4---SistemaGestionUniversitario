using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class SistemaPermisosYFuncionesAcademicas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotaExamen_IdExamen",
                table: "NotaExamen");

            migrationBuilder.AlterColumn<string>(
                name: "NombreUsuario",
                table: "Usuarios",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "DebeCambiarPassword",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "Nota",
                table: "NotaExamen",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateTable(
                name: "DocentesMaterias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdDocente = table.Column<int>(type: "int", nullable: false),
                    IdMateria = table.Column<int>(type: "int", nullable: false),
                    CicloLectivo = table.Column<int>(type: "int", nullable: false),
                    Cuatrimestre = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FechaAsignacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocentesMaterias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocentesMaterias_Materias_IdMateria",
                        column: x => x.IdMateria,
                        principalTable: "Materias",
                        principalColumn: "IdMateria",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocentesMaterias_Usuarios_IdDocente",
                        column: x => x.IdDocente,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Permisos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosAsistenciaPersonal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    HoraEntrada = table.Column<TimeOnly>(type: "time", nullable: true),
                    HoraSalida = table.Column<TimeOnly>(type: "time", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegistradoPorUsuarioId = table.Column<int>(type: "int", nullable: false),
                    FechaRegistroUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAsistenciaPersonal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosAsistenciaPersonal_Usuarios_RegistradoPorUsuarioId",
                        column: x => x.RegistradoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrosAsistenciaPersonal_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolesPermisos",
                columns: table => new
                {
                    RolId = table.Column<int>(type: "int", nullable: false),
                    PermisoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolesPermisos", x => new { x.RolId, x.PermisoId });
                    table.ForeignKey(
                        name: "FK_RolesPermisos_Permisos_PermisoId",
                        column: x => x.PermisoId,
                        principalTable: "Permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolesPermisos_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Codigo", "Descripcion" },
                values: new object[,]
                {
                    { 1, "usuarios.leer", "Consultar usuarios." },
                    { 2, "usuarios.gestionar", "Crear y administrar cuentas." },
                    { 3, "academico.leer", "Consultar carreras, años y materias." },
                    { 4, "academico.gestionar", "Administrar la estructura académica." },
                    { 5, "inscripciones.propias.leer", "Consultar sus materias." },
                    { 6, "inscripciones.gestionar", "Administrar inscripciones y asignaciones." },
                    { 7, "docencia.gestionar", "Gestionar las comisiones asignadas." },
                    { 8, "asistencias.propias.leer", "Consultar sus asistencias." },
                    { 9, "asistencia-personal.gestionar", "Registrar fichado del personal." },
                    { 10, "examenes.propios.leer", "Consultar sus evaluaciones." },
                    { 11, "notas.propias.leer", "Consultar sus calificaciones." },
                    { 12, "reportes.leer", "Consultar reportes institucionales." }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Descripcion", "Nombre" },
                values: new object[,]
                {
                    { 1, "Acceso únicamente a su información académica.", "Estudiante" },
                    { 2, "Gestión de sus comisiones, asistencias, evaluaciones y notas.", "Docente" },
                    { 3, "Administración académica y de cuentas institucionales.", "Secretario" },
                    { 4, "Consulta institucional y reportes.", "Directivo" }
                });

            migrationBuilder.InsertData(
                table: "RolesPermisos",
                columns: new[] { "PermisoId", "RolId" },
                values: new object[,]
                {
                    { 3, 1 },
                    { 5, 1 },
                    { 8, 1 },
                    { 10, 1 },
                    { 11, 1 },
                    { 3, 2 },
                    { 7, 2 },
                    { 1, 3 },
                    { 2, 3 },
                    { 3, 3 },
                    { 4, 3 },
                    { 5, 3 },
                    { 6, 3 },
                    { 7, 3 },
                    { 8, 3 },
                    { 9, 3 },
                    { 10, 3 },
                    { 11, 3 },
                    { 12, 3 },
                    { 1, 4 },
                    { 3, 4 },
                    { 12, 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NombreUsuario",
                table: "Usuarios",
                column: "NombreUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Rol",
                table: "Usuarios",
                column: "Rol");

            migrationBuilder.CreateIndex(
                name: "IX_Personas_Dni",
                table: "Personas",
                column: "Dni",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotaExamen_IdExamen_IdEstudiante",
                table: "NotaExamen",
                columns: new[] { "IdExamen", "IdEstudiante" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocentesMaterias_IdDocente_IdMateria_CicloLectivo_Cuatrimestre",
                table: "DocentesMaterias",
                columns: new[] { "IdDocente", "IdMateria", "CicloLectivo", "Cuatrimestre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocentesMaterias_IdMateria",
                table: "DocentesMaterias",
                column: "IdMateria");

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_Codigo",
                table: "Permisos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAsistenciaPersonal_RegistradoPorUsuarioId",
                table: "RegistrosAsistenciaPersonal",
                column: "RegistradoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAsistenciaPersonal_UsuarioId_Fecha",
                table: "RegistrosAsistenciaPersonal",
                columns: new[] { "UsuarioId", "Fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolesPermisos_PermisoId",
                table: "RolesPermisos",
                column: "PermisoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Roles_Rol",
                table: "Usuarios",
                column: "Rol",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Roles_Rol",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "DocentesMaterias");

            migrationBuilder.DropTable(
                name: "RegistrosAsistenciaPersonal");

            migrationBuilder.DropTable(
                name: "RolesPermisos");

            migrationBuilder.DropTable(
                name: "Permisos");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_NombreUsuario",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Rol",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Personas_Dni",
                table: "Personas");

            migrationBuilder.DropIndex(
                name: "IX_NotaExamen_IdExamen_IdEstudiante",
                table: "NotaExamen");

            migrationBuilder.DropColumn(
                name: "DebeCambiarPassword",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "NombreUsuario",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Nota",
                table: "NotaExamen",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(4,2)",
                oldPrecision: 4,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_NotaExamen_IdExamen",
                table: "NotaExamen",
                column: "IdExamen");
        }
    }
}
