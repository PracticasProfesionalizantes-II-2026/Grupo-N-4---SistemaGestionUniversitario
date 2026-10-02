using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaAcciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosAuditoria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: true),
                    NombreUsuario = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Metodo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Ruta = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EstadoHttp = table.Column<int>(type: "int", nullable: false),
                    Ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAuditoria", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_FechaUtc",
                table: "RegistrosAuditoria",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_UsuarioId_FechaUtc",
                table: "RegistrosAuditoria",
                columns: new[] { "UsuarioId", "FechaUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosAuditoria");
        }
    }
}
