using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class AsistenciaDocentePorMateria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RegistrosAsistenciaPersonal_UsuarioId_Fecha",
                table: "RegistrosAsistenciaPersonal");

            migrationBuilder.AddColumn<int>(
                name: "MateriaId",
                table: "RegistrosAsistenciaPersonal",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAsistenciaPersonal_MateriaId",
                table: "RegistrosAsistenciaPersonal",
                column: "MateriaId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAsistenciaPersonal_UsuarioId_Fecha",
                table: "RegistrosAsistenciaPersonal",
                columns: new[] { "UsuarioId", "Fecha" },
                unique: true,
                filter: "[MateriaId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAsistenciaPersonal_UsuarioId_MateriaId_Fecha",
                table: "RegistrosAsistenciaPersonal",
                columns: new[] { "UsuarioId", "MateriaId", "Fecha" },
                unique: true,
                filter: "[MateriaId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrosAsistenciaPersonal_Materias_MateriaId",
                table: "RegistrosAsistenciaPersonal",
                column: "MateriaId",
                principalTable: "Materias",
                principalColumn: "IdMateria",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RegistrosAsistenciaPersonal_Materias_MateriaId",
                table: "RegistrosAsistenciaPersonal");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosAsistenciaPersonal_MateriaId",
                table: "RegistrosAsistenciaPersonal");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosAsistenciaPersonal_UsuarioId_Fecha",
                table: "RegistrosAsistenciaPersonal");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosAsistenciaPersonal_UsuarioId_MateriaId_Fecha",
                table: "RegistrosAsistenciaPersonal");

            migrationBuilder.DropColumn(
                name: "MateriaId",
                table: "RegistrosAsistenciaPersonal");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAsistenciaPersonal_UsuarioId_Fecha",
                table: "RegistrosAsistenciaPersonal",
                columns: new[] { "UsuarioId", "Fecha" },
                unique: true);
        }
    }
}
