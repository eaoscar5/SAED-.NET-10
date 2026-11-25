using Microsoft.EntityFrameworkCore;
using saed.api.Model;

namespace saed.api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        #region DbSets
        public DbSet<Proceso> Procesos { get; set; }
        public DbSet<Academia> Academias { get; set; }
        public DbSet<Instrumento> Instrumentos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Pregunta> Preguntas { get; set; }
        public DbSet<OpcionRespuesta> OpcionesRespuesta { get; set; }
        public DbSet<Evaluacion> Evaluaciones { get; set; }
        public DbSet<ComprobanteFinalizacion> ComprobantesFinalizacion { get; set; }
        public DbSet<Respuesta> Respuestas { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<ProcesoInstrumento> ProcesoInstrumentos { get; set; }
        public DbSet<MaestroAcademia> MaestrosAcademia { get; set; }
        public DbSet<MateriaCatalogo> MateriasCatalogo { get; set; }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            #region Instrumento � grafo completo

            modelBuilder.Entity<Instrumento>().ToTable("Instrumentos");
            modelBuilder.Entity<Categoria>().ToTable("Categorias");
            modelBuilder.Entity<Pregunta>().ToTable("Preguntas");
            modelBuilder.Entity<OpcionRespuesta>().ToTable("OpcionesRespuesta");

            // Conversiones de enum a string
            modelBuilder.Entity<Instrumento>()
                .Property(i => i.TipoInstrumento)
                .HasConversion<string>();

            modelBuilder.Entity<Pregunta>()
                .Property(p => p.Tipo)
                .HasConversion<string>();

            // Categoria  Instrumento
            modelBuilder.Entity<Categoria>()
                .HasOne(c => c.Instrumento)
                .WithMany(i => i.Categorias)
                .HasForeignKey(c => c.InstrumentoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Pregunta  Categoria
            modelBuilder.Entity<Pregunta>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Preguntas)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Cascade);

            // OpcionRespuesta  Pregunta
            modelBuilder.Entity<OpcionRespuesta>()
                .HasOne(o => o.Pregunta)
                .WithMany(p => p.Opciones)
                .HasForeignKey(o => o.PreguntaId)
                .OnDelete(DeleteBehavior.Cascade);

            // �ndices del grafo de instrumento
            modelBuilder.Entity<Categoria>()
                .HasIndex(c => c.InstrumentoId);

            modelBuilder.Entity<Pregunta>()
                .HasIndex(p => p.CategoriaId);

            modelBuilder.Entity<OpcionRespuesta>()
                .HasIndex(o => o.PreguntaId);

            // Longitudes y restricciones
            modelBuilder.Entity<Instrumento>()
                .Property(i => i.Nombre)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<Categoria>()
                .Property(c => c.Nombre)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<Pregunta>()
                .Property(p => p.Texto)
                .HasMaxLength(500)
                .IsRequired();

            modelBuilder.Entity<OpcionRespuesta>()
                .Property(o => o.Texto)
                .HasMaxLength(300)
                .IsRequired();

            #endregion

            #region Proceso

            modelBuilder.Entity<Proceso>().ToTable("Procesos");

            // Proceso  Instrumento  (la relaci�n ahora vive en Proceso)
            modelBuilder.Entity<Proceso>()
                .HasOne(p => p.Instrumento)
                .WithMany()
                .HasForeignKey(p => p.InstrumentoId)
                .OnDelete(DeleteBehavior.SetNull);  // si se elimina el instrumento, el proceso queda sin �l

            modelBuilder.Entity<Proceso>()
                .HasIndex(p => p.InstrumentoId);

            modelBuilder.Entity<ProcesoInstrumento>().ToTable("ProcesoInstrumentos");

            modelBuilder.Entity<ProcesoInstrumento>()
                .HasKey(pi => new { pi.ProcesoId, pi.InstrumentoId });

            modelBuilder.Entity<ProcesoInstrumento>()
                .HasOne(pi => pi.Proceso)
                .WithMany(p => p.ProcesoInstrumentos)
                .HasForeignKey(pi => pi.ProcesoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProcesoInstrumento>()
                .HasOne(pi => pi.Instrumento)
                .WithMany(i => i.ProcesoInstrumentos)
                .HasForeignKey(pi => pi.InstrumentoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProcesoInstrumento>()
                .HasIndex(pi => new { pi.ProcesoId, pi.Orden })
                .IsUnique();

            modelBuilder.Entity<Proceso>()
                .Property(p => p.Nombre)
                .HasMaxLength(200)
                .IsRequired();

            #endregion
            #region Catalogos universidad

            modelBuilder.Entity<Academia>().ToTable("Academias");
            modelBuilder.Entity<Academia>()
                .Property(a => a.AcaClave)
                .HasMaxLength(20)
                .IsRequired();
            modelBuilder.Entity<Academia>()
                .Property(a => a.Nombre)
                .HasMaxLength(200)
                .IsRequired();
            modelBuilder.Entity<Academia>()
                .HasIndex(a => a.AcaClave)
                .IsUnique();

            modelBuilder.Entity<MaestroAcademia>().ToTable("MaestrosAcademia");
            modelBuilder.Entity<MaestroAcademia>()
                .Property(m => m.AcaClave)
                .HasMaxLength(20)
                .IsRequired();
            modelBuilder.Entity<MaestroAcademia>()
                .Property(m => m.NombreAcademia)
                .HasMaxLength(200)
                .IsRequired();
            modelBuilder.Entity<MaestroAcademia>()
                .Property(m => m.IdMaestro)
                .HasMaxLength(20)
                .IsRequired();
            modelBuilder.Entity<MaestroAcademia>()
                .Property(m => m.NombreMaestro)
                .HasMaxLength(200)
                .IsRequired();
            modelBuilder.Entity<MaestroAcademia>()
                .HasIndex(m => new { m.AcaClave, m.IdMaestro })
                .IsUnique();
            modelBuilder.Entity<MaestroAcademia>()
                .HasIndex(m => m.IdMaestro);

            modelBuilder.Entity<MateriaCatalogo>().ToTable("MateriasCatalogo");
            modelBuilder.Entity<MateriaCatalogo>()
                .Property(m => m.MateriaId)
                .HasMaxLength(20)
                .IsRequired();
            modelBuilder.Entity<MateriaCatalogo>()
                .Property(m => m.NombreMateria)
                .HasMaxLength(200)
                .IsRequired();
            modelBuilder.Entity<MateriaCatalogo>()
                .Property(m => m.Seriacion)
                .HasMaxLength(50);
            modelBuilder.Entity<MateriaCatalogo>()
                .Property(m => m.CarreraClave)
                .HasMaxLength(50);
            modelBuilder.Entity<MateriaCatalogo>()
                .HasIndex(m => m.MateriaId)
                .IsUnique();
            modelBuilder.Entity<MateriaCatalogo>()
                .HasIndex(m => new { m.TipoMateria, m.NaturalezaNoCurricular });
            modelBuilder.Entity<MateriaCatalogo>()
                .HasIndex(m => m.CarreraClave);

            #endregion
            #region Evaluacion

            modelBuilder.Entity<Evaluacion>().ToTable("Evaluaciones");
            modelBuilder.Entity<Respuesta>().ToTable("Respuestas");
            modelBuilder.Entity<Comentario>().ToTable("Comentarios");

            modelBuilder.Entity<Evaluacion>()
                .Property(e => e.TipoInstrumento)
                .HasConversion<string>();

            // Evaluacion - Proceso
            modelBuilder.Entity<Evaluacion>()
                .HasOne(e => e.Proceso)
                .WithMany()
                .HasForeignKey(e => e.ProcesoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indice unico: un alumno no puede evaluar dos veces la misma asignacion
            // academica validada dentro del mismo proceso e instrumento.
            modelBuilder.Entity<Evaluacion>()
                .HasIndex(e => new
                {
                    e.ProcesoId,
                    e.MatriculaAlumno,
                    e.AsignacionAcademicaId,
                    e.InstrumentoId
                })
                .IsUnique()
                .HasFilter("\"InstrumentoId\" IS NOT NULL")
                .HasDatabaseName("IX_Evaluaciones_Duplicado_Alumno_Asignacion_Instrumento");
            modelBuilder.Entity<Evaluacion>()
                .HasIndex(e => new { e.ProcesoId, e.TipoInstrumento });
            modelBuilder.Entity<Evaluacion>()
                .HasIndex(e => new { e.ProcesoId, e.AcademiaClave });
            modelBuilder.Entity<Evaluacion>()
                .HasIndex(e => new { e.ProcesoId, e.CarreraClave });

            modelBuilder.Entity<ComprobanteFinalizacion>().ToTable("ComprobantesFinalizacion");
            modelBuilder.Entity<ComprobanteFinalizacion>()
                .Property(c => c.MatriculaAlumno)
                .HasMaxLength(20)
                .IsRequired();
            modelBuilder.Entity<ComprobanteFinalizacion>()
                .Property(c => c.Grupo)
                .HasMaxLength(120)
                .IsRequired();
            modelBuilder.Entity<ComprobanteFinalizacion>()
                .Property(c => c.Folio)
                .HasMaxLength(40)
                .IsRequired();
            modelBuilder.Entity<ComprobanteFinalizacion>()
                .HasIndex(c => new { c.ProcesoId, c.MatriculaAlumno })
                .IsUnique();
            modelBuilder.Entity<ComprobanteFinalizacion>()
                .HasIndex(c => c.Folio)
                .IsUnique();
            modelBuilder.Entity<ComprobanteFinalizacion>()
                .HasOne(c => c.Proceso)
                .WithMany()
                .HasForeignKey(c => c.ProcesoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Respuesta - Evaluacion
            modelBuilder.Entity<Respuesta>()
                .HasOne(r => r.Evaluacion)
                .WithMany(e => e.Respuestas)
                .HasForeignKey(r => r.EvaluacionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Respuesta - Pregunta
            modelBuilder.Entity<Respuesta>()
                .HasOne(r => r.Pregunta)
                .WithMany()
                .HasForeignKey(r => r.PreguntaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Respuesta - OpcionRespuesta (navegacion para reportes)
            modelBuilder.Entity<Respuesta>()
                .HasOne(r => r.Opcion)
                .WithMany()
                .HasForeignKey(r => r.OpcionId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Respuesta>()
                .HasIndex(r => new { r.EvaluacionId, r.PreguntaId })
                .IsUnique();

            // Comentario - Evaluacion (1 a 1)
            modelBuilder.Entity<Comentario>()
                .HasOne(c => c.Evaluacion)
                .WithOne(e => e.Comentario)
                .HasForeignKey<Comentario>(c => c.EvaluacionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Campos requeridos de Evaluacion
            modelBuilder.Entity<Evaluacion>()
                .Property(e => e.MatriculaAlumno).HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Evaluacion>()
                .Property(e => e.AsignacionAcademicaId).HasMaxLength(120).IsRequired();
            modelBuilder.Entity<Evaluacion>()
                .Property(e => e.IdMaestro).HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Evaluacion>()
                .Property(e => e.NombreMaestro).HasMaxLength(200).IsRequired();
            modelBuilder.Entity<Evaluacion>()
                .Property(e => e.MateriaId).HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Evaluacion>()
                .Property(e => e.NombreMateria).HasMaxLength(200).IsRequired();
            modelBuilder.Entity<Evaluacion>()
                .Property(e => e.Grupo).HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Evaluacion>()
                .Property(e => e.AcademiaClave).HasMaxLength(20);
            modelBuilder.Entity<Evaluacion>()
                .Property(e => e.AcademiaNombre).HasMaxLength(200);
            modelBuilder.Entity<Evaluacion>()
                .Property(e => e.CarreraClave).HasMaxLength(50);
            modelBuilder.Entity<Respuesta>()
                .Property(r => r.Texto).HasMaxLength(2000);

            #endregion
        }
    }
}
