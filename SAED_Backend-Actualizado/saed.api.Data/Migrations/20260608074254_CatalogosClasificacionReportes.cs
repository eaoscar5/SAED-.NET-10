using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace saed.api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CatalogosClasificacionReportes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcademiaClave",
                table: "Evaluaciones",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcademiaNombre",
                table: "Evaluaciones",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CarreraClave",
                table: "Evaluaciones",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstrumentoId",
                table: "Evaluaciones",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MateriaNoCurricular",
                table: "Evaluaciones",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TipoInstrumento",
                table: "Evaluaciones",
                type: "text",
                nullable: false,
                defaultValue: "Curricular");

            migrationBuilder.AddColumn<int>(
                name: "TipoMateria",
                table: "Evaluaciones",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Academias",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "AcaClave",
                table: "Academias",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"Academias\" SET \"AcaClave\" = CONCAT('LEGACY_', \"Id\") " +
                "WHERE \"AcaClave\" IS NULL OR \"AcaClave\" = '';");

            migrationBuilder.AlterColumn<string>(
                name: "AcaClave",
                table: "Academias",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsExtracurricular",
                table: "Academias",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaSincronizacionUtc",
                table: "Academias",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaestrosAcademia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AcaClave = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NombreAcademia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IdMaestro = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NombreMaestro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FechaSincronizacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaestrosAcademia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MateriasCatalogo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MateriaId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NombreMateria = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HorasTotales = table.Column<int>(type: "integer", nullable: false),
                    NaturalezaNoCurricular = table.Column<bool>(type: "boolean", nullable: false),
                    TipoMateria = table.Column<int>(type: "integer", nullable: false),
                    Seriacion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CarreraClave = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NumeroCompetencias = table.Column<int>(type: "integer", nullable: false),
                    MateriaIntegradora = table.Column<bool>(type: "boolean", nullable: false),
                    FechaSincronizacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MateriasCatalogo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Evaluaciones_ProcesoId_AcademiaClave",
                table: "Evaluaciones",
                columns: new[] { "ProcesoId", "AcademiaClave" });

            migrationBuilder.CreateIndex(
                name: "IX_Evaluaciones_ProcesoId_CarreraClave",
                table: "Evaluaciones",
                columns: new[] { "ProcesoId", "CarreraClave" });

            migrationBuilder.CreateIndex(
                name: "IX_Evaluaciones_ProcesoId_TipoInstrumento",
                table: "Evaluaciones",
                columns: new[] { "ProcesoId", "TipoInstrumento" });

            migrationBuilder.CreateIndex(
                name: "IX_Academias_AcaClave",
                table: "Academias",
                column: "AcaClave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaestrosAcademia_AcaClave_IdMaestro",
                table: "MaestrosAcademia",
                columns: new[] { "AcaClave", "IdMaestro" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaestrosAcademia_IdMaestro",
                table: "MaestrosAcademia",
                column: "IdMaestro");

            migrationBuilder.CreateIndex(
                name: "IX_MateriasCatalogo_CarreraClave",
                table: "MateriasCatalogo",
                column: "CarreraClave");

            migrationBuilder.CreateIndex(
                name: "IX_MateriasCatalogo_MateriaId",
                table: "MateriasCatalogo",
                column: "MateriaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MateriasCatalogo_TipoMateria_NaturalezaNoCurricular",
                table: "MateriasCatalogo",
                columns: new[] { "TipoMateria", "NaturalezaNoCurricular" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaestrosAcademia");

            migrationBuilder.DropTable(
                name: "MateriasCatalogo");

            migrationBuilder.DropIndex(
                name: "IX_Evaluaciones_ProcesoId_AcademiaClave",
                table: "Evaluaciones");

            migrationBuilder.DropIndex(
                name: "IX_Evaluaciones_ProcesoId_CarreraClave",
                table: "Evaluaciones");

            migrationBuilder.DropIndex(
                name: "IX_Evaluaciones_ProcesoId_TipoInstrumento",
                table: "Evaluaciones");

            migrationBuilder.DropIndex(
                name: "IX_Academias_AcaClave",
                table: "Academias");

            migrationBuilder.DropColumn(
                name: "AcademiaClave",
                table: "Evaluaciones");

            migrationBuilder.DropColumn(
                name: "AcademiaNombre",
                table: "Evaluaciones");

            migrationBuilder.DropColumn(
                name: "CarreraClave",
                table: "Evaluaciones");

            migrationBuilder.DropColumn(
                name: "InstrumentoId",
                table: "Evaluaciones");

            migrationBuilder.DropColumn(
                name: "MateriaNoCurricular",
                table: "Evaluaciones");

            migrationBuilder.DropColumn(
                name: "TipoInstrumento",
                table: "Evaluaciones");

            migrationBuilder.DropColumn(
                name: "TipoMateria",
                table: "Evaluaciones");

            migrationBuilder.DropColumn(
                name: "AcaClave",
                table: "Academias");

            migrationBuilder.DropColumn(
                name: "EsExtracurricular",
                table: "Academias");

            migrationBuilder.DropColumn(
                name: "FechaSincronizacionUtc",
                table: "Academias");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Academias",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }
    }
}
