using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class NotificacionesAutomaticasYReportes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CreadaPorUsuarioId",
                table: "NotificacionesGenerales",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "ClaveAutomatica",
                table: "NotificacionesGenerales",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioDestinoId",
                table: "NotificacionesGenerales",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesGenerales_ClaveAutomatica",
                table: "NotificacionesGenerales",
                column: "ClaveAutomatica",
                unique: true,
                filter: "[ClaveAutomatica] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacionesGenerales_UsuarioDestinoId",
                table: "NotificacionesGenerales",
                column: "UsuarioDestinoId");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificacionesGenerales_Usuarios_UsuarioDestinoId",
                table: "NotificacionesGenerales",
                column: "UsuarioDestinoId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificacionesGenerales_Usuarios_UsuarioDestinoId",
                table: "NotificacionesGenerales");

            migrationBuilder.DropIndex(
                name: "IX_NotificacionesGenerales_ClaveAutomatica",
                table: "NotificacionesGenerales");

            migrationBuilder.DropIndex(
                name: "IX_NotificacionesGenerales_UsuarioDestinoId",
                table: "NotificacionesGenerales");

            migrationBuilder.DropColumn(
                name: "ClaveAutomatica",
                table: "NotificacionesGenerales");

            migrationBuilder.DropColumn(
                name: "UsuarioDestinoId",
                table: "NotificacionesGenerales");

            migrationBuilder.AlterColumn<int>(
                name: "CreadaPorUsuarioId",
                table: "NotificacionesGenerales",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
