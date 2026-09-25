using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcadionApi.Migrations
{
    /// <inheritdoc />
    public partial class PerfilEstudianteYAllegados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Allegados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstudianteId = table.Column<int>(type: "int", nullable: false),
                    NombreApellido = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Relacion = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allegados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Allegados_Usuarios_EstudianteId",
                        column: x => x.EstudianteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerfilesFinanciamiento",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    AporteFamiliares = table.Column<bool>(type: "bit", nullable: false),
                    PlanesSociales = table.Column<bool>(type: "bit", nullable: false),
                    Trabajo = table.Column<bool>(type: "bit", nullable: false),
                    Beca = table.Column<bool>(type: "bit", nullable: false),
                    OtraFuente = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilesFinanciamiento", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_PerfilesFinanciamiento_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Allegados_EstudianteId_NombreApellido",
                table: "Allegados",
                columns: new[] { "EstudianteId", "NombreApellido" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Allegados");

            migrationBuilder.DropTable(
                name: "PerfilesFinanciamiento");
        }
    }
}
