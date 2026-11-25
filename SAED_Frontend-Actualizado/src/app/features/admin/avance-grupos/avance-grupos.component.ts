import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from 'src/app/core/services/api.service';
import { descargarArchivo, prepararDescargaArchivo } from 'src/app/core/utils/file-download.util';
import { agregarLogoUtsPdf, obtenerLogoUtsPdf } from 'src/app/core/utils/pdf-logo.util';

interface ProcesoDto {
  id: number;
  nombre: string;
  anio: number;
  periodo: number;
  activo: boolean;
}

interface CatalogoItem {
  clave: string;
  nombre: string;
  academiaClave?: string | null;
  carreraClave?: string | null;
}

interface ReporteCatalogos {
  academias: CatalogoItem[];
  carreras: CatalogoItem[];
  grupos: CatalogoItem[];
}

interface ProgresoGrupoResumen {
  procesoId: number;
  nombreProceso: string;
  anio: number;
  periodo: number;
  grupo: string;
  nombreGrupo: string;
  cuatrimestre: number;
  academiaClave: string;
  academiaNombre: string;
  carreraClave: string;
  carreraNombre: string;
  totalAlumnos: number;
  alumnosEvaluaron: number;
  alumnosFaltantes: number;
  totalEvaluacionesRegistradas: number;
  porcentajeEvaluacion: number;
}

interface ProgresoMateria {
  idMaestro: string;
  nombreMaestro: string;
  materiaId: string;
  nombreMateria: string;
  tipoInstrumento: 'Curricular' | 'Extracurricular';
}

interface ProgresoAlumno {
  matricula: string;
  nombreAlumno: string;
  evaluacionesEsperadas: number;
  evaluacionesRealizadas: number;
  evaluacionesFaltantes: number;
  evaluado: boolean;
  ultimaEvaluacion?: string | null;
}

interface ProgresoGrupoDetalle extends ProgresoGrupoResumen {
  materiasAsignadas: number;
  materias: ProgresoMateria[];
  alumnos: ProgresoAlumno[];
}

@Component({
  selector: 'app-avance-grupos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './avance-grupos.component.html',
  styleUrl: './avance-grupos.component.scss'
})
export class AvanceGruposComponent implements OnInit {
  procesos: ProcesoDto[] = [];
  academias: CatalogoItem[] = [];
  carreras: CatalogoItem[] = [];
  grupos: CatalogoItem[] = [];
  resultados: ProgresoGrupoResumen[] = [];
  detalle: ProgresoGrupoDetalle | null = null;

  procesoSeleccionado: number | null = null;
  academiaSeleccionada = '';
  carreraSeleccionada = '';
  grupoSeleccionado = '';

  loadingCatalogos = false;
  buscando = false;
  cargandoDetalle = false;
  generandoPdf = false;
  buscado = false;
  errorMsg = '';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.cargarProcesos();
  }

  cargarProcesos(): void {
    this.api.get<ProcesoDto[]>('/procesos').subscribe({
      next: (res) => {
        this.procesos = res ?? [];
        this.procesoSeleccionado = this.obtenerProcesoPredeterminado()?.id ?? null;
        this.cargarCatalogos(() => this.buscar());
      },
      error: () => {
        this.errorMsg = 'No se pudieron cargar los períodos.';
      }
    });
  }

  cargarCatalogos(alTerminar?: () => void): void {
    this.loadingCatalogos = true;
    this.api.get<ReporteCatalogos>('/reportes/catalogos', {
      procesoId: this.procesoSeleccionado || undefined
    }).subscribe({
      next: (res) => {
        this.academias = res.academias ?? [];
        this.carreras = res.carreras ?? [];
        this.grupos = res.grupos ?? [];
        this.reconciliarFiltrosCatalogo();
        this.loadingCatalogos = false;
        alTerminar?.();
      },
      error: () => {
        this.academias = [];
        this.carreras = [];
        this.grupos = [];
        this.loadingCatalogos = false;
        this.errorMsg = 'No se pudieron cargar los catálogos para el período.';
        alTerminar?.();
      }
    });
  }

  alCambiarProceso(): void {
    this.academiaSeleccionada = '';
    this.carreraSeleccionada = '';
    this.grupoSeleccionado = '';
    this.resultados = [];
    this.detalle = null;
    this.buscado = false;
    this.cargarCatalogos();
  }

  alCambiarAcademia(): void {
    if (!this.contieneClave(this.carrerasDisponibles, this.carreraSeleccionada))
      this.carreraSeleccionada = '';

    this.validarGrupoSeleccionado();
  }

  alCambiarCarrera(): void {
    this.validarGrupoSeleccionado();
  }

  buscar(): void {
    this.buscando = true;
    this.buscado = true;
    this.detalle = null;
    this.errorMsg = '';

    this.api.get<ProgresoGrupoResumen[]>('/reportes/progreso/grupos', this.buildParams()).subscribe({
      next: (res) => {
        this.resultados = res ?? [];
        this.buscando = false;
      },
      error: (err) => {
        this.resultados = [];
        this.errorMsg = err?.error?.message ?? 'Error al consultar el avance por grupo.';
        this.buscando = false;
      }
    });
  }

  verDetalle(row: ProgresoGrupoResumen): void {
    this.cargandoDetalle = true;
    this.detalle = null;
    this.errorMsg = '';

    this.api.get<ProgresoGrupoDetalle>(
      `/reportes/progreso/grupos/${encodeURIComponent(row.grupo)}`,
      { procesoId: row.procesoId }
    ).subscribe({
      next: (res) => {
        this.detalle = res;
        this.cargandoDetalle = false;
        setTimeout(() => document.querySelector('.detalle-progreso')?.scrollIntoView({
          behavior: 'smooth',
          block: 'start'
        }));
      },
      error: (err) => {
        this.errorMsg = err?.error?.message ?? 'Error al consultar el detalle del grupo.';
        this.cargandoDetalle = false;
      }
    });
  }

  limpiar(): void {
    this.procesoSeleccionado = null;
    this.academiaSeleccionada = '';
    this.carreraSeleccionada = '';
    this.grupoSeleccionado = '';
    this.resultados = [];
    this.detalle = null;
    this.buscado = false;
    this.errorMsg = '';
    this.cargarCatalogos();
  }

  cerrarDetalle(): void {
    this.detalle = null;
  }

  periodoLabel(periodo: number): string {
    return ({ 1: 'Sep-Dic', 2: 'Ene-Abr', 3: 'May-Ago' } as Record<number, string>)[periodo]
      ?? String(periodo);
  }

  tipoLabel(tipo: string): string {
    return tipo === 'Extracurricular' ? 'Extracurricular' : 'Curricular';
  }

  get carrerasDisponibles(): CatalogoItem[] {
    if (!this.tieneRelacionesDeGrupo()) return this.carreras;

    const claves = new Set(
      this.grupos
        .filter(grupo => this.coincideClave(grupo.academiaClave, this.academiaSeleccionada))
        .map(grupo => this.normalizarClave(grupo.carreraClave))
        .filter((clave): clave is string => clave.length > 0)
    );

    return this.carreras.filter(carrera => claves.has(this.normalizarClave(carrera.clave)));
  }

  get gruposDisponibles(): CatalogoItem[] {
    if (!this.tieneRelacionesDeGrupo()) return this.grupos;

    return this.grupos.filter(grupo =>
      this.coincideClave(grupo.academiaClave, this.academiaSeleccionada) &&
      this.coincideClave(grupo.carreraClave, this.carreraSeleccionada));
  }

  estadoAlumnoLabel(alumno: ProgresoAlumno): string {
    if (alumno.evaluado) return 'Evaluado';
    if (alumno.evaluacionesRealizadas > 0) return 'Parcial';
    return 'Pendiente';
  }

  fechaCorta(value?: string | null): string {
    if (!value) return '-';
    return new Date(value).toLocaleDateString('es-MX');
  }

  async exportarPdf(): Promise<void> {
    if (!this.detalle) return;

    this.generandoPdf = true;
    this.errorMsg = '';
    const nombreArchivo = this.sanitizarNombre(
      `avance_grupo_${this.detalle.grupo}_${this.detalle.anio}_${this.detalle.periodo}.pdf`
    );
    const descargaPreparada = prepararDescargaArchivo(nombreArchivo);

    try {
      const { default: jsPDF } = await import('jspdf');
      const { default: autoTable } = await import('jspdf-autotable');
      const logoUts = await obtenerLogoUtsPdf();
      const doc = new jsPDF({ unit: 'pt', format: 'letter' });
      const grupo = this.detalle;
      const margen = 40;
      const encabezadoX = logoUts ? 150 : margen;

      agregarLogoUtsPdf(doc, logoUts, margen, 18, 96);

      doc.setFont('helvetica', 'bold');
      doc.setFontSize(15);
      doc.text('Reporte de avance de evaluación por grupo', encabezadoX, 42);

      doc.setFont('helvetica', 'normal');
      doc.setFontSize(9);
      doc.text(`${grupo.nombreProceso} · ${this.periodoLabel(grupo.periodo)} ${grupo.anio}`, encabezadoX, 60);
      doc.text(`Grupo ${grupo.grupo} · ${grupo.nombreGrupo}`, encabezadoX, 74);
      doc.text(`${grupo.academiaNombre} · ${grupo.carreraNombre}`, encabezadoX, 88);

      autoTable(doc, {
        startY: 110,
        head: [['Alumnos', 'Evaluaron', 'Faltan', 'Avance', 'Evaluaciones registradas']],
        body: [[
          String(grupo.totalAlumnos),
          String(grupo.alumnosEvaluaron),
          String(grupo.alumnosFaltantes),
          `${grupo.porcentajeEvaluacion.toFixed(2)} %`,
          String(grupo.totalEvaluacionesRegistradas)
        ]],
        styles: { fontSize: 8, cellPadding: 5 },
        headStyles: { fillColor: [0, 105, 92] }
      });

      autoTable(doc, {
        startY: (doc as any).lastAutoTable.finalY + 18,
        head: [['Materia', 'Maestro', 'Tipo']],
        body: grupo.materias.map(m => [
          `${m.nombreMateria} (${m.materiaId})`,
          `${m.nombreMaestro} (${m.idMaestro})`,
          this.tipoLabel(m.tipoInstrumento)
        ]),
        styles: { fontSize: 7.5, cellPadding: 4 },
        headStyles: { fillColor: [31, 41, 55] }
      });

      autoTable(doc, {
        startY: (doc as any).lastAutoTable.finalY + 18,
        head: [['Estado', 'Matrícula', 'Alumno', 'Realizadas', 'Faltantes', 'Última evaluación']],
        body: grupo.alumnos.map(a => [
          a.evaluado ? '' : this.estadoAlumnoLabel(a),
          a.matricula,
          a.nombreAlumno,
          `${a.evaluacionesRealizadas}/${a.evaluacionesEsperadas}`,
          String(a.evaluacionesFaltantes),
          this.fechaCorta(a.ultimaEvaluacion)
        ]),
        styles: { fontSize: 7.2, cellPadding: 3 },
        headStyles: { fillColor: [0, 105, 92] },
        columnStyles: {
          0: { cellWidth: 58 },
          1: { cellWidth: 72 },
          2: { cellWidth: 190 }
        },
        didDrawCell: (data: any) => {
          if (data.section !== 'body' || data.column.index !== 0) return;

          const alumno = grupo.alumnos[data.row.index] as ProgresoAlumno | undefined;
          if (!alumno?.evaluado) return;

          const centroX = data.cell.x + data.cell.width / 2;
          const centroY = data.cell.y + data.cell.height / 2;

          doc.setFillColor(22, 163, 74);
          doc.circle(centroX, centroY, 3.6, 'F');
          doc.setDrawColor(255, 255, 255);
          doc.setLineWidth(0.8);
          doc.line(centroX - 1.7, centroY, centroX - 0.35, centroY + 1.45);
          doc.line(centroX - 0.35, centroY + 1.45, centroX + 2.1, centroY - 1.55);
        }
      });

      await descargarArchivo(
        doc.output('blob') as Blob,
        nombreArchivo,
        descargaPreparada
      );
    } catch (err: any) {
      this.errorMsg = err?.message ?? 'No se pudo generar el PDF del grupo.';
    } finally {
      this.generandoPdf = false;
    }
  }

  private buildParams(): Record<string, any> {
    return {
      procesoId: this.procesoSeleccionado || undefined,
      academiaClave: this.academiaSeleccionada || undefined,
      carreraClave: this.carreraSeleccionada || undefined,
      grupo: this.grupoSeleccionado || undefined
    };
  }

  private obtenerProcesoPredeterminado(): ProcesoDto | null {
    return this.procesos
      .slice()
      .sort((a, b) =>
        Number(b.activo) - Number(a.activo)
        || b.anio - a.anio
        || b.periodo - a.periodo
        || b.id - a.id)[0] ?? null;
  }

  private reconciliarFiltrosCatalogo(): void {
    if (!this.contieneClave(this.academias, this.academiaSeleccionada))
      this.academiaSeleccionada = '';

    if (!this.contieneClave(this.carrerasDisponibles, this.carreraSeleccionada))
      this.carreraSeleccionada = '';

    this.validarGrupoSeleccionado();
  }

  private validarGrupoSeleccionado(): void {
    if (!this.contieneClave(this.gruposDisponibles, this.grupoSeleccionado))
      this.grupoSeleccionado = '';
  }

  private tieneRelacionesDeGrupo(): boolean {
    return this.grupos.some(grupo =>
      !!this.normalizarClave(grupo.academiaClave) || !!this.normalizarClave(grupo.carreraClave));
  }

  private contieneClave(catalogo: CatalogoItem[], clave: string): boolean {
    return !clave || catalogo.some(item => this.normalizarClave(item.clave) === this.normalizarClave(clave));
  }

  private coincideClave(valor: string | null | undefined, filtro: string): boolean {
    return !filtro || this.normalizarClave(valor) === this.normalizarClave(filtro);
  }

  private normalizarClave(valor: string | null | undefined): string {
    return (valor ?? '').trim().toUpperCase();
  }

  private sanitizarNombre(value: string): string {
    return value.replace(/[<>:"/\\|?*\x00-\x1F]/g, '_');
  }
}
