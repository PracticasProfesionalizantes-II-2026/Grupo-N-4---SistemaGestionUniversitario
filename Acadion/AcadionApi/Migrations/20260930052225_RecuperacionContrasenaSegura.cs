using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class RecuperacionContrasenaSegura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecuperacionesContrasena",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CodigoHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaExpiracionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaVerificacionUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaExpiracionTokenUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaUsoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Intentos = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecuperacionesContrasena", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecuperacionesContrasena_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecuperacionesContrasena_TokenHash",
                table: "RecuperacionesContrasena",
                column: "TokenHash",
                filter: "[TokenHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RecuperacionesContrasena_UsuarioId_FechaExpiracionUtc",
                table: "RecuperacionesContrasena",
                columns: new[] { "UsuarioId", "FechaExpiracionUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecuperacionesContrasena");
        }
    }
}
