using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class DocumentosLecturasYJustificaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaJustificacionUtc",
                table: "RegistrosAsistenciaPersonal",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JustificadaPorUsuarioId",
                table: "RegistrosAsistenciaPersonal",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JustificativoArchivo",
                table: "RegistrosAsistenciaPersonal",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaJustificacionUtc",
                table: "Asistencia",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JustificadaPorUsuarioId",
                table: "Asistencia",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JustificativoArchivo",
                table: "Asistencia",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "NotificacionesLecturas",
                columns: table => new
                {
                    NotificacionId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    FechaLecturaUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacionesLecturas", x => new { x.NotificacionId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_NotificacionesLecturas_NotificacionesGenerales_NotificacionId",
                        column: x => x.NotificacionId,
                        principalTable: "NotificacionesGenerales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificacionesLecturas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAsistenciaPersonal_JustificadaPorUsuarioId",
                table: "RegistrosAsistenciaPersonal",
                column: "JustificadaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencia_JustificadaPorUsuarioId",
                table: "Asistencia",
                column: "JustificadaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesLecturas_UsuarioId",
                table: "NotificacionesLecturas",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Asistencia_Usuarios_JustificadaPorUsuarioId",
                table: "Asistencia",
                column: "JustificadaPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrosAsistenciaPersonal_Usuarios_JustificadaPorUsuarioId",
                table: "RegistrosAsistenciaPersonal",
                column: "JustificadaPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asistencia_Usuarios_JustificadaPorUsuarioId",
                table: "Asistencia");

            migrationBuilder.DropForeignKey(
                name: "FK_RegistrosAsistenciaPersonal_Usuarios_JustificadaPorUsuarioId",
                table: "RegistrosAsistenciaPersonal");

            migrationBuilder.DropTable(
                name: "NotificacionesLecturas");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosAsistenciaPersonal_JustificadaPorUsuarioId",
                table: "RegistrosAsistenciaPersonal");

            migrationBuilder.DropIndex(
                name: "IX_Asistencia_JustificadaPorUsuarioId",
                table: "Asistencia");

            migrationBuilder.DropColumn(
                name: "FechaJustificacionUtc",
                table: "RegistrosAsistenciaPersonal");

            migrationBuilder.DropColumn(
                name: "JustificadaPorUsuarioId",
                table: "RegistrosAsistenciaPersonal");

            migrationBuilder.DropColumn(
                name: "JustificativoArchivo",
                table: "RegistrosAsistenciaPersonal");

            migrationBuilder.DropColumn(
                name: "FechaJustificacionUtc",
                table: "Asistencia");

            migrationBuilder.DropColumn(
                name: "JustificadaPorUsuarioId",
                table: "Asistencia");

            migrationBuilder.DropColumn(
                name: "JustificativoArchivo",
                table: "Asistencia");
        }
    }
}
