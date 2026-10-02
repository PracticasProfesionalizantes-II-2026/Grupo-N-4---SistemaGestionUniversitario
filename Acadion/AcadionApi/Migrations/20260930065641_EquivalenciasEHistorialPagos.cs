using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class EquivalenciasEHistorialPagos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaValidacionUtc",
                table: "MatriculasIniciales",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Importe",
                table: "MatriculasIniciales",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MetodoPago",
                table: "MatriculasIniciales",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "MatriculasIniciales",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ValidadoPorUsuarioId",
                table: "MatriculasIniciales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaValidacionUtc",
                table: "CuotasMensuales",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Importe",
                table: "CuotasMensuales",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MetodoPago",
                table: "CuotasMensuales",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "CuotasMensuales",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ValidadoPorUsuarioId",
                table: "CuotasMensuales",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EquivalenciasMaterias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstudianteId = table.Column<int>(type: "int", nullable: false),
                    MateriaOrigenId = table.Column<int>(type: "int", nullable: false),
                    MateriaDestinoId = table.Column<int>(type: "int", nullable: false),
                    FechaOtorgamientoUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OtorgadaPorUsuarioId = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquivalenciasMaterias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquivalenciasMaterias_Materias_MateriaDestinoId",
                        column: x => x.MateriaDestinoId,
                        principalTable: "Materias",
                        principalColumn: "IdMateria",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquivalenciasMaterias_Materias_MateriaOrigenId",
                        column: x => x.MateriaOrigenId,
                        principalTable: "Materias",
                        principalColumn: "IdMateria",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquivalenciasMaterias_Usuarios_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquivalenciasMaterias_Usuarios_OtorgadaPorUsuarioId",
                        column: x => x.OtorgadaPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatriculasIniciales_ValidadoPorUsuarioId",
                table: "MatriculasIniciales",
                column: "ValidadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CuotasMensuales_ValidadoPorUsuarioId",
                table: "CuotasMensuales",
                column: "ValidadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EquivalenciasMaterias_EstudianteId_MateriaDestinoId",
                table: "EquivalenciasMaterias",
                columns: new[] { "EstudianteId", "MateriaDestinoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquivalenciasMaterias_MateriaDestinoId",
                table: "EquivalenciasMaterias",
                column: "MateriaDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_EquivalenciasMaterias_MateriaOrigenId",
                table: "EquivalenciasMaterias",
                column: "MateriaOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_EquivalenciasMaterias_OtorgadaPorUsuarioId",
                table: "EquivalenciasMaterias",
                column: "OtorgadaPorUsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_CuotasMensuales_Usuarios_ValidadoPorUsuarioId",
                table: "CuotasMensuales",
                column: "ValidadoPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MatriculasIniciales_Usuarios_ValidadoPorUsuarioId",
                table: "MatriculasIniciales",
                column: "ValidadoPorUsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CuotasMensuales_Usuarios_ValidadoPorUsuarioId",
                table: "CuotasMensuales");

            migrationBuilder.DropForeignKey(
                name: "FK_MatriculasIniciales_Usuarios_ValidadoPorUsuarioId",
                table: "MatriculasIniciales");

            migrationBuilder.DropTable(
                name: "EquivalenciasMaterias");

            migrationBuilder.DropIndex(
                name: "IX_MatriculasIniciales_ValidadoPorUsuarioId",
                table: "MatriculasIniciales");

            migrationBuilder.DropIndex(
                name: "IX_CuotasMensuales_ValidadoPorUsuarioId",
                table: "CuotasMensuales");

            migrationBuilder.DropColumn(
                name: "FechaValidacionUtc",
                table: "MatriculasIniciales");

            migrationBuilder.DropColumn(
                name: "Importe",
                table: "MatriculasIniciales");

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "MatriculasIniciales");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "MatriculasIniciales");

            migrationBuilder.DropColumn(
                name: "ValidadoPorUsuarioId",
                table: "MatriculasIniciales");

            migrationBuilder.DropColumn(
                name: "FechaValidacionUtc",
                table: "CuotasMensuales");

            migrationBuilder.DropColumn(
                name: "Importe",
                table: "CuotasMensuales");

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "CuotasMensuales");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "CuotasMensuales");

            migrationBuilder.DropColumn(
                name: "ValidadoPorUsuarioId",
                table: "CuotasMensuales");
        }
    }
}
