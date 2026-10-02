using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class VincularRecuperatoriosAEvaluaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExamenRecuperadoId",
                table: "Examen",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Examen_ExamenRecuperadoId",
                table: "Examen",
                column: "ExamenRecuperadoId",
                unique: true,
                filter: "[ExamenRecuperadoId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Examen_Examen_ExamenRecuperadoId",
                table: "Examen",
                column: "ExamenRecuperadoId",
                principalTable: "Examen",
                principalColumn: "IdExamen",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Examen_Examen_ExamenRecuperadoId",
                table: "Examen");

            migrationBuilder.DropIndex(
                name: "IX_Examen_ExamenRecuperadoId",
                table: "Examen");

            migrationBuilder.DropColumn(
                name: "ExamenRecuperadoId",
                table: "Examen");
        }
    }
}
