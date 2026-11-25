using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace saed.api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProcesosMultiplesInstrumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcesoInstrumentos",
                columns: table => new
                {
                    ProcesoId = table.Column<int>(type: "integer", nullable: false),
                    InstrumentoId = table.Column<int>(type: "integer", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcesoInstrumentos", x => new { x.ProcesoId, x.InstrumentoId });
                    table.ForeignKey(
                        name: "FK_ProcesoInstrumentos_Instrumentos_InstrumentoId",
                        column: x => x.InstrumentoId,
                        principalTable: "Instrumentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProcesoInstrumentos_Procesos_ProcesoId",
                        column: x => x.ProcesoId,
                        principalTable: "Procesos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO "ProcesoInstrumentos" ("ProcesoId", "InstrumentoId", "Orden")
                SELECT "Id", "InstrumentoId", 1
                FROM "Procesos"
                WHERE "InstrumentoId" IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ProcesoInstrumentos_InstrumentoId",
                table: "ProcesoInstrumentos",
                column: "InstrumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcesoInstrumentos_ProcesoId_Orden",
                table: "ProcesoInstrumentos",
                columns: new[] { "ProcesoId", "Orden" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcesoInstrumentos");
        }
    }
}
