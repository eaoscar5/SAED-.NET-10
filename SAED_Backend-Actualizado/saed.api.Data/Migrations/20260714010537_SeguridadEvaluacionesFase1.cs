using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace saed.api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeguridadEvaluacionesFase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Respuestas_EvaluacionId",
                table: "Respuestas");

            migrationBuilder.DropIndex(
                name: "IX_Evaluaciones_ProcesoId_MatriculaAlumno_IdMaestro_MateriaId",
                table: "Evaluaciones");

            migrationBuilder.AlterColumn<int>(
                name: "OpcionId",
                table: "Respuestas",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Texto",
                table: "Respuestas",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AsignacionAcademicaId",
                table: "Evaluaciones",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "Evaluaciones"
                SET "AsignacionAcademicaId" =
                    UPPER(TRIM(COALESCE("Grupo", ''))) || '|' ||
                    UPPER(TRIM(COALESCE("IdMaestro", ''))) || '|' ||
                    UPPER(TRIM(COALESCE("MateriaId", '')))
                WHERE "AsignacionAcademicaId" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Respuestas_EvaluacionId_PreguntaId",
                table: "Respuestas",
                columns: new[] { "EvaluacionId", "PreguntaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evaluaciones_Duplicado_Alumno_Asignacion_Instrumento",
                table: "Evaluaciones",
                columns: new[] { "ProcesoId", "MatriculaAlumno", "AsignacionAcademicaId", "InstrumentoId" },
                unique: true,
                filter: "\"InstrumentoId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Respuestas_EvaluacionId_PreguntaId",
                table: "Respuestas");

            migrationBuilder.DropIndex(
                name: "IX_Evaluaciones_Duplicado_Alumno_Asignacion_Instrumento",
                table: "Evaluaciones");

            migrationBuilder.DropColumn(
                name: "Texto",
                table: "Respuestas");

            migrationBuilder.DropColumn(
                name: "AsignacionAcademicaId",
                table: "Evaluaciones");

            migrationBuilder.AlterColumn<int>(
                name: "OpcionId",
                table: "Respuestas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Respuestas_EvaluacionId",
                table: "Respuestas",
                column: "EvaluacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluaciones_ProcesoId_MatriculaAlumno_IdMaestro_MateriaId",
                table: "Evaluaciones",
                columns: new[] { "ProcesoId", "MatriculaAlumno", "IdMaestro", "MateriaId" },
                unique: true);
        }
    }
}
