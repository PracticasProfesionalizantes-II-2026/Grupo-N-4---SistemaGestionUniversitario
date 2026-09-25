using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class NotificacionesGenerales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificacionesGenerales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RolDestinoId = table.Column<int>(type: "int", nullable: true),
                    Prioridad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaPublicacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaExpiracionUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    CreadaPorUsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacionesGenerales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificacionesGenerales_Roles_RolDestinoId",
                        column: x => x.RolDestinoId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificacionesGenerales_Usuarios_CreadaPorUsuarioId",
                        column: x => x.CreadaPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Codigo", "Descripcion" },
                values: new object[] { 13, "notificaciones.gestionar", "Publicar y administrar notificaciones generales." });

            migrationBuilder.InsertData(
                table: "RolesPermisos",
                columns: new[] { "PermisoId", "RolId" },
                values: new object[] { 13, 3 });

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesGenerales_CreadaPorUsuarioId",
                table: "NotificacionesGenerales",
                column: "CreadaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesGenerales_RolDestinoId",
                table: "NotificacionesGenerales",
                column: "RolDestinoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificacionesGenerales");

            migrationBuilder.DeleteData(
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { 13, 3 });

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 13);
        }
    }
}
