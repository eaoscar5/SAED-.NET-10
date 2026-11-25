using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace saed.api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarComprobantesFinalizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComprobantesFinalizacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProcesoId = table.Column<int>(type: "integer", nullable: false),
                    MatriculaAlumno = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Grupo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Folio = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FechaFinalizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprobantesFinalizacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComprobantesFinalizacion_Procesos_ProcesoId",
                        column: x => x.ProcesoId,
                        principalTable: "Procesos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComprobantesFinalizacion_Folio",
                table: "ComprobantesFinalizacion",
                column: "Folio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComprobantesFinalizacion_ProcesoId_MatriculaAlumno",
                table: "ComprobantesFinalizacion",
                columns: new[] { "ProcesoId", "MatriculaAlumno" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComprobantesFinalizacion");
        }
    }
}
