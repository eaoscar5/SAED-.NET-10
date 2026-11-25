import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from 'src/app/core/services/api.service';
import { environment } from 'src/environments/environment';
import { descargarArchivo, prepararDescargaArchivo } from 'src/app/core/utils/file-download.util';
import { agregarLogoUtsPdf, obtenerLogoUtsPdf } from 'src/app/core/utils/pdf-logo.util';
import {
  AlcanceExportacionZip,
  crearRutaReporteZip,
  obtenerUnidadesExportacionZip,
  reservarRutaZip
} from './reportes-exportacion.util';

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
  catalogosLocalesDisponibles?: boolean;
  sincronizacionRequerida?: boolean;
  sincronizacionIntentada?: boolean;
  gruposDesdeUniversidad?: boolean;
  fechaUltimaSincronizacionUtc?: string | null;
  mensaje?: string | null;
}

interface SincronizacionCatalogos {
  academias: number;
  maestrosAcademia: number;
  materias: number;
  fechaSincronizacionUtc: string;
}

interface ReporteMockSeed {
  procesoId: number;
  nombreProceso: string;
  academias: number;
  maestrosAcademia: number;
  materias: number;
  evaluaciones: number;
  reportes: number;
  message: string;
}

interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

interface ReporteResumen {
  procesoId: number;
  nombreProceso: string;
  anio: number;
  periodo: number;
  idMaestro: string;
  nombreMaestro: string;
  materiaId: string;
  nombreMateria: string;
  grupo: string;
  tipoInstrumento: 'Curricular' | 'Extracurricular';
  instrumentoId?: number | null;
  academiaClave?: string | null;
  academiaNombre?: string | null;
  carreraClave?: string | null;
  totalEvaluaciones: number;
  calificacionPromedio: number;
  ultimaEvaluacion: string;
}

interface CalificacionPregunta {
  preguntaId: number;
  orden: number;
  categoriaId: number;
  categoriaOrden: number;
  categoriaNombre: string;
  textoPregunta: string;
  promedio: number;
  totalRespuestas: number;
  distribucion: Record<string, number>;
}

interface CalificacionPorMateria {
  materiaId: string;
  nombreMateria: string;
  grupo: string;
  tipoInstrumento?: 'Curricular' | 'Extracurricular';
  instrumentoId?: number | null;
  academiaClave?: string | null;
  academiaNombre?: string | null;
  carreraClave?: string | null;
  totalEvaluaciones: number;
  calificacion: number;
  porPregunta: CalificacionPregunta[];
  comentarios: string[];
}

interface CalificacionMaestro {
  idMaestro: string;
  nombreMaestro: string;
  procesoId: number;
  materiaId?: string | null;
  grupo?: string | null;
  instrumentoId?: number | null;
  filtradoPorMateria?: boolean;
  tipoInstrumento?: 'Curricular' | 'Extracurricular' | null;
  totalEvaluaciones: number;
  calificacionGlobal: number;
  porMateria: CalificacionPorMateria[];
}

type AlcanceReporte = 'global' | 'materia';

interface ResumenOpcionPdf {
  etiqueta: string;
  votos: number;
  porcentaje: number;
  color: string;
}

interface ReportePreguntaFilaDetalle {
  materiaId: string;
  nombreMateria: string;
  grupo: string;
  tipoInstrumento?: 'Curricular' | 'Extracurricular';
  academiaNombre?: string | null;
  carreraClave?: string | null;
  totalEvaluaciones: number;
  promedio: number;
  promedioEscala10: number;
  distribucion: Record<string, number>;
  distribucionTexto: string;
}

interface ReportePreguntaDetalle {
  preguntaId: number;
  orden: number;
  textoPregunta: string;
  promedio: number;
  promedioEscala10: number;
  totalRespuestas: number;
  filas: ReportePreguntaFilaDetalle[];
}

interface ReporteCategoriaDetalle {
  nombre: string;
  orden: number;
  promedio: number;
  promedioEscala10: number;
  preguntas: ReportePreguntaDetalle[];
}

@Component({
  selector: 'app-maestros-proceso',
  standalone: true,
  templateUrl: './maestros-proceso.component.html',
  styleUrl: './maestros-proceso.component.scss',
  imports: [CommonModule, FormsModule]
})
export class MaestrosProcesoComponent implements OnInit {

  procesos: ProcesoDto[] = [];
  academias: CatalogoItem[] = [];
  carreras: CatalogoItem[] = [];
  grupos: CatalogoItem[] = [];
  reportes: ReporteResumen[] = [];
  reporte: CalificacionMaestro | null = null;
  nombreProceso = '';
  materiaActiva = 0;
  reporteAlcance: AlcanceReporte = 'global';
  resumenVisualReporte: ResumenOpcionPdf[] = [];
  resumenVisualGradient = '#e9edf2';
  resumenVisualTotalVotos = 0;
  detalleCategoriasReporte: ReporteCategoriaDetalle[] = [];

  procesoSeleccionado: number | null = null;
  academiaSeleccionada = '';
  carreraSeleccionada = '';
  grupoSeleccionado = '';
  tipoInstrumento: '' | 'Curricular' | 'Extracurricular' = '';
  nombreBusqueda = '';
  vistaGlobal = false;
  zipAlcance: AlcanceExportacionZip = 'global';

  page = 1;
  pageSize = 25;
  total = 0;
  buscado = false;

  loading = false;
  loadingReporte = false;
  loadingCatalogos = false;
  sincronizando = false;
  sincronizacionCatalogosIntentada = false;
  sembrandoMock = false;
  sembrandoMockDetallado = false;
  generandoPdf = false;
  generandoZip = false;
  catalogosLocalesDisponibles = true;
  sincronizacionRequerida = false;
  gruposDesdeUniversidad = false;
  fechaUltimaSincronizacionUtc: string | null = null;
  catalogosMensaje = '';
  errorMsg = '';
  infoMsg = '';
  mostrarDatosDemo = !environment.production;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.cargarProcesos();
    this.cargarCatalogos();
  }

  cargarProcesos(): void {
    this.loading = true;
    this.api.get<ProcesoDto[]>('/procesos').subscribe({
      next: (res) => {
        this.procesos = res;
        this.loading = false;
      },
      error: () => {
        this.errorMsg = 'No se pudieron cargar los procesos.';
        this.loading = false;
      }
    });
  }

  cargarCatalogos(): void {
    this.loadingCatalogos = true;
    this.api.get<ReporteCatalogos>('/reportes/catalogos', {
      procesoId: this.procesoSeleccionado || undefined
    }).subscribe({
      next: (res) => {
        this.academias = res.academias ?? [];
        this.carreras = res.carreras ?? [];
        this.grupos = res.grupos ?? [];
        this.reconciliarFiltrosCatalogo();
        this.catalogosLocalesDisponibles = res.catalogosLocalesDisponibles ?? true;
        this.sincronizacionRequerida = res.sincronizacionRequerida ?? false;
        this.gruposDesdeUniversidad = res.gruposDesdeUniversidad ?? false;
        this.fechaUltimaSincronizacionUtc = res.fechaUltimaSincronizacionUtc ?? null;
        this.catalogosMensaje = res.mensaje ?? '';
        this.loadingCatalogos = false;
      },
      error: (err) => {
        this.academias = [];
        this.carreras = [];
        this.grupos = [];
        this.catalogosLocalesDisponibles = false;
        this.sincronizacionRequerida = true;
        this.gruposDesdeUniversidad = false;
        this.fechaUltimaSincronizacionUtc = null;
        this.catalogosMensaje = err?.error?.message ?? 'No se pudieron cargar los catálogos de universidad.';
        this.loadingCatalogos = false;
      }
    });
  }

  alCambiarProceso(): void {
    this.academiaSeleccionada = '';
    this.carreraSeleccionada = '';
    this.grupoSeleccionado = '';
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

  sincronizarCatalogos(alTerminar?: () => void, automatica = false): void {
    if (this.sincronizando) return;

    this.sincronizacionCatalogosIntentada = true;
    this.sincronizando = true;
    this.errorMsg = '';
    this.infoMsg = '';
    this.catalogosMensaje = '';
    this.api.post<SincronizacionCatalogos>('/universidad/catalogos/sincronizar', {}).subscribe({
      next: (res) => {
        this.sincronizando = false;
        this.catalogosLocalesDisponibles = true;
        this.sincronizacionRequerida = false;
        this.fechaUltimaSincronizacionUtc = res.fechaSincronizacionUtc;
        this.infoMsg = `Catálogos sincronizados correctamente: ${res.academias} academias, ${res.maestrosAcademia} maestros por academia y ${res.materias} materias.`;
        this.cargarCatalogos();
        alTerminar?.();
      },
      error: (err) => {
        this.sincronizando = false;
        if (automatica) {
          this.sincronizacionRequerida = true;
          this.catalogosMensaje = err?.error?.message
            ?? 'No se pudieron actualizar los catálogos. Se consultarán los datos disponibles.';
          alTerminar?.();
          return;
        }

        this.errorMsg = err?.error?.message ?? 'No se pudieron sincronizar los catálogos.';
      }
    });
  }

  sembrarDatosMock(): void {
    this.sembrandoMock = true;
    this.errorMsg = '';
    this.infoMsg = '';

    this.api.post<ReporteMockSeed>('/reportes/mock/sembrar', {}).subscribe({
      next: (res) => {
        this.sembrandoMock = false;
        this.vistaGlobal = true;
        this.procesoSeleccionado = res.procesoId;
        this.cargarProcesos();
        this.cargarCatalogos();
        this.buscar(1, false, true);
        this.infoMsg = `${res.message} ${res.academias} academias, ${res.materias} materias, ${res.evaluaciones} evaluaciones y ${res.reportes} reportes.`;
      },
      error: (err) => {
        this.sembrandoMock = false;
        this.errorMsg = err?.error?.message ?? 'No se pudieron generar los datos demo.';
      }
    });
  }

  sembrarDatosMockDetallado(): void {
    this.sembrandoMockDetallado = true;
    this.errorMsg = '';
    this.infoMsg = '';

    this.api.post<ReporteMockSeed>('/reportes/mock/sembrar-detallado', {}).subscribe({
      next: (res) => {
        this.sembrandoMockDetallado = false;
        this.vistaGlobal = true;
        this.procesoSeleccionado = res.procesoId;
        this.nombreBusqueda = '1434';
        this.cargarProcesos();
        this.cargarCatalogos();
        this.buscar(1, false, true);
        this.infoMsg = `${res.message} ${res.evaluaciones} evaluaciones en ${res.reportes} combinaciones de docente, grupo y materia.`;
      },
      error: (err) => {
        this.sembrandoMockDetallado = false;
        this.errorMsg = err?.error?.message ?? 'No se pudo generar el reporte demo detallado.';
      }
    });
  }

  buscar(page = 1, actualizarCatalogos = true, permitirMientrasCargaCatalogos = false): void {
    if (this.loading || this.sincronizando || (this.loadingCatalogos && !permitirMientrasCargaCatalogos)) return;

    if (actualizarCatalogos && !this.sincronizacionCatalogosIntentada) {
      this.sincronizacionCatalogosIntentada = true;
      this.sincronizarCatalogos(() => this.ejecutarBusqueda(page), true);
      return;
    }

    this.ejecutarBusqueda(page);
  }

  private ejecutarBusqueda(page: number): void {
    this.page = page;
    this.errorMsg = '';
    this.infoMsg = '';
    this.loading = true;
    this.buscado = true;

    this.api.get<PagedResult<ReporteResumen>>('/reportes/buscar', this.buildParams(this.page, this.pageSize)).subscribe({
      next: (res) => {
        this.reportes = res.items ?? [];
        this.total = res.total ?? 0;
        this.loading = false;
      },
      error: (err) => {
        this.reportes = [];
        this.total = 0;
        this.errorMsg = err?.error?.message ?? 'Error al buscar reportes.';
        this.loading = false;
      }
    });
  }

  limpiar(): void {
    this.procesoSeleccionado = null;
    this.academiaSeleccionada = '';
    this.carreraSeleccionada = '';
    this.grupoSeleccionado = '';
    this.tipoInstrumento = '';
    this.nombreBusqueda = '';
    this.vistaGlobal = false;
    this.reportes = [];
    this.total = 0;
    this.page = 1;
    this.buscado = false;
    this.reporte = null;
    this.actualizarResumenVisual(null);
    this.errorMsg = '';
    this.infoMsg = '';
  }

  verReporte(row: ReporteResumen, alcance: AlcanceReporte = 'global'): void {
    this.loadingReporte = true;
    this.reporte = null;
    this.actualizarResumenVisual(null);
    this.materiaActiva = 0;
    this.reporteAlcance = alcance;
    this.nombreProceso = alcance === 'materia'
      ? `${row.nombreProceso} · ${this.periodoLabel(row.periodo)} ${row.anio} · ${row.nombreMateria} · Grupo ${row.grupo}`
      : `${row.nombreProceso} · ${this.periodoLabel(row.periodo)} ${row.anio}`;

    const params = alcance === 'materia'
      ? {
          materiaId: row.materiaId,
          grupo: row.grupo,
          tipoInstrumento: row.tipoInstrumento,
          instrumentoId: row.instrumentoId ?? undefined
        }
      : undefined;

    this.api.get<CalificacionMaestro>(
      `/calificaciones/procesos/${row.procesoId}/maestros/${row.idMaestro}`,
      params
    ).subscribe({
      next: (res) => {
        this.reporte = res;
        this.actualizarResumenVisual(res);
        this.loadingReporte = false;
      },
      error: () => {
        this.errorMsg = 'Error al cargar el reporte.';
        this.actualizarResumenVisual(null);
        this.loadingReporte = false;
      }
    });
  }

  cerrarReporte(): void {
    this.reporte = null;
    this.actualizarResumenVisual(null);
    this.reporteAlcance = 'global';
  }

  get totalPaginas(): number {
    return Math.max(1, Math.ceil(this.total / this.pageSize));
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

  get colorCalificacion(): string {
    return this.getColorEscala10(this.calificacionEscala10);
  }

  get calificacionEscala10(): number {
    return Math.round(((this.reporte?.calificacionGlobal ?? 0) / 5) * 10 * 100) / 100;
  }

  get etiquetaAlcanceReporte(): string {
    return this.reporteAlcance === 'materia'
      ? 'Reporte de materia'
      : 'Reporte global del maestro';
  }

  get etiquetaPromedioReporte(): string {
    return this.reporteAlcance === 'materia'
      ? 'Calificación de Materia'
      : 'Calificación General';
  }

  getColor(val: number): string {
    if (val >= 4) return '#01907e';
    if (val >= 3) return '#f59e0b';
    return '#e11d48';
  }

  getColorEscala10(val: number): string {
    if (val >= 8) return '#01907e';
    if (val >= 6) return '#f59e0b';
    return '#e11d48';
  }

  distribucionEntries(dist: Record<string, number>): { key: string; value: number }[] {
    return Object.entries(dist).map(([key, value]) => ({ key, value }));
  }

  porcentajeResumen(item: ResumenOpcionPdf): string {
    return `${item.porcentaje.toFixed(item.porcentaje === 0 ? 0 : 2)} %`;
  }

  periodoLabel(p: number): string {
    return ({ 1: 'Sep-Dic', 2: 'Ene-Abr', 3: 'May-Ago' } as any)[p] ?? String(p);
  }

  tipoLabel(tipo: string): string {
    return tipo === 'Extracurricular' ? 'Extracurricular' : 'Curricular';
  }

  async exportarPdf(): Promise<void> {
    if (!this.reporte) return;
    this.generandoPdf = true;
    this.errorMsg = '';
    const detalle = this.reporte.porMateria[0];
    const materia = this.reporteAlcance === 'materia' && detalle
      ? `_${detalle.materiaId}_grupo_${detalle.grupo}_${detalle.tipoInstrumento ?? 'sin_tipo'}${detalle.instrumentoId == null ? '' : `_instrumento_${detalle.instrumentoId}`}`
      : '_global';
    const nombreArchivo = this.sanitizarNombre(
      `reporte_${this.reporte.idMaestro}_p${this.reporte.procesoId}${materia}.pdf`
    );
    const descargaPreparada = prepararDescargaArchivo(nombreArchivo);

    try {
      const doc = await this.construirPdf(this.reporte, this.nombreProceso, this.reporteAlcance);

      await descargarArchivo(
        doc.output('blob') as Blob,
        nombreArchivo,
        descargaPreparada
      );
    } catch (err: any) {
      this.errorMsg = err?.message ?? 'No se pudo generar el PDF.';
    } finally {
      this.generandoPdf = false;
    }
  }

  async exportarZip(): Promise<void> {
    if (this.total === 0) return;
    this.generandoZip = true;
    this.errorMsg = '';
    const alcance = this.zipAlcance;
    const prefijo = alcance === 'global' ? 'reportes' : 'reportes_por_materia_grupo';
    const nombreArchivo = `${prefijo}_${new Date().toISOString().slice(0, 10)}.zip`;
    const descargaPreparada = prepararDescargaArchivo(nombreArchivo);

    try {
      const JSZipModule = await import('jszip');
      const JSZipCtor = (JSZipModule as any).default ?? JSZipModule;
      const zip = new JSZipCtor();
      const rows = await this.obtenerTodosLosResultados();
      const unidades = obtenerUnidadesExportacionZip(rows, alcance);
      const rutasUsadas = new Set<string>();

      for (const row of unidades) {
        const reporte = await this.obtenerReporte(row, alcance);
        const periodo = this.periodoLabel(row.periodo);
        const nombreProceso = alcance === 'materia'
          ? `${row.nombreProceso} · ${periodo} ${row.anio} · ${row.nombreMateria} · Grupo ${row.grupo} · ${this.tipoLabel(row.tipoInstrumento)}`
          : `${row.nombreProceso} · ${periodo} ${row.anio}`;
        const doc = await this.construirPdf(reporte, nombreProceso, alcance);
        const blob = doc.output('blob') as Blob;
        const ruta = reservarRutaZip(
          crearRutaReporteZip(row, periodo, alcance),
          rutasUsadas
        );
        zip.file(ruta, blob);
      }

      const contenido = await zip.generateAsync({ type: 'blob' });
      await descargarArchivo(contenido, nombreArchivo, descargaPreparada);
    } catch (err: any) {
      this.errorMsg = err?.message ?? 'No se pudo generar el ZIP.';
    } finally {
      this.generandoZip = false;
    }
  }

  private buildParams(page: number, pageSize: number): Record<string, any> {
    return {
      procesoId: this.procesoSeleccionado || undefined,
      academiaClave: this.academiaSeleccionada || undefined,
      carreraClave: this.carreraSeleccionada || undefined,
      grupo: this.grupoSeleccionado || undefined,
      nombreMaestro: this.nombreBusqueda.trim() || undefined,
      tipoInstrumento: this.tipoInstrumento || undefined,
      vistaGlobal: this.vistaGlobal,
      page,
      pageSize
    };
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

  private async obtenerTodosLosResultados(): Promise<ReporteResumen[]> {
    const pageSize = 100;
    const totalPages = Math.max(1, Math.ceil(this.total / pageSize));
    const rows: ReporteResumen[] = [];

    for (let page = 1; page <= totalPages; page++) {
      const res = await this.api
        .get<PagedResult<ReporteResumen>>('/reportes/buscar', this.buildParams(page, pageSize))
        .toPromise();

      rows.push(...(res?.items ?? []));
    }

    return rows;
  }

  private obtenerReporte(row: ReporteResumen, alcance: AlcanceReporte = 'global'): Promise<CalificacionMaestro> {
    const params = alcance === 'materia'
      ? {
          materiaId: row.materiaId,
          grupo: row.grupo,
          tipoInstrumento: row.tipoInstrumento,
          instrumentoId: row.instrumentoId ?? undefined
        }
      : undefined;

    return new Promise((resolve, reject) => {
      this.api.get<CalificacionMaestro>(
        `/calificaciones/procesos/${row.procesoId}/maestros/${row.idMaestro}`,
        params
      ).subscribe({
        next: resolve,
        error: reject
      });
    });
  }

  private async construirPdf(
    reporte: CalificacionMaestro,
    nombreProceso: string,
    alcance: AlcanceReporte = 'global'
  ): Promise<any> {
    const { default: jsPDF } = await import('jspdf');
    const { default: autoTable } = await import('jspdf-autotable');
    const logoUts = await obtenerLogoUtsPdf();

    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    const margen = 15;
    let y = margen;

    agregarLogoUtsPdf(doc, logoUts, 7, 4, 34);
    doc.setTextColor(0, 0, 0);
    doc.setFontSize(14);
    doc.setFont('helvetica', 'bold');
    doc.text(
      alcance === 'materia'
        ? 'REPORTE DE MATERIA DOCENTE'
        : 'REPORTE GLOBAL DE EVALUACIÓN DOCENTE',
      105,
      13,
      { align: 'center' }
    );
    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    doc.text('Universidad Tecnológica del Sur de Sonora', 105, 21, { align: 'center' });
    doc.text(nombreProceso, 105, 27, { align: 'center' });
    y = 38;

    doc.setTextColor(31, 45, 41);
    doc.setFontSize(13);
    doc.setFont('helvetica', 'bold');
    doc.text(reporte.nombreMaestro, margen, y);
    y += 7;
    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(69, 83, 79);
    doc.text(`ID: ${reporte.idMaestro}`, margen, y);
    doc.text(`Total evaluaciones: ${reporte.totalEvaluaciones}`, 110, y);
    y += 8;

    const cal = Math.round((reporte.calificacionGlobal / 5) * 10 * 100) / 100;
    const [r, g, b] = cal >= 8 ? [1, 144, 126] : cal >= 6 ? [245, 158, 11] : [225, 29, 72];
    doc.setFillColor(r, g, b);
    doc.roundedRect(margen, y, 90, 14, 3, 3, 'F');
    doc.setTextColor(255, 255, 255);
    doc.setFontSize(11);
    doc.setFont('helvetica', 'bold');
    doc.text(`${alcance === 'materia' ? 'Calificación por materia' : 'Calificación general'}: ${cal.toFixed(2)}/10  (${reporte.calificacionGlobal.toFixed(2)}/5)`,
      margen + 45, y + 9, { align: 'center' });
    y += 20;

    y = this.agregarResumenVisualPdf(doc, reporte, alcance, y, margen);
    y = this.agregarDetalleCategoriasPdf(doc, autoTable, reporte, y, margen);
    y = this.agregarComentariosPdf(doc, reporte, y, margen);

    const total = (doc as any).internal.getNumberOfPages();
    for (let i = 1; i <= total; i++) {
      doc.setPage(i);
      doc.setFontSize(7);
      doc.setTextColor(150);
      doc.text(`Generado el ${new Date().toLocaleDateString('es-MX')} · Página ${i} de ${total}`, 105, 292, { align: 'center' });
    }

    return doc;
  }

  private agregarResumenVisualPdf(
    doc: any,
    reporte: CalificacionMaestro,
    alcance: AlcanceReporte,
    y: number,
    margen: number
  ): number {
    const ancho = 180;
    const alto = 86;

    if (y + alto > 270) {
      doc.addPage();
      y = margen;
    }

    const resumen = this.construirResumenOpcionesPdf(reporte);
    const calificacion = Math.round((reporte.calificacionGlobal / 5) * 10 * 100) / 100;
    const tituloCalificacion = alcance === 'materia'
      ? 'Calificación por materia'
      : 'Calificación general';
    const image = this.crearImagenResumenPdf(resumen, `${tituloCalificacion}: ${calificacion.toFixed(2)}`);

    doc.addImage(image, 'PNG', margen, y, ancho, alto);
    return y + alto + 8;
  }

  private agregarDetalleCategoriasPdf(
    doc: any,
    autoTable: any,
    reporte: CalificacionMaestro,
    y: number,
    margen: number
  ): number {
    const detalle = this.construirDetalleCategorias(reporte);

    if (detalle.length === 0) return y;

    if (y > 250) {
      doc.addPage();
      y = margen;
    }

    doc.setTextColor(31, 45, 41);
    doc.setFontSize(11);
    doc.setFont('helvetica', 'bold');
    doc.text('DETALLE POR CATEGORÍA, PREGUNTA, GRUPO Y MATERIA', margen, y);
    y += 7;

    detalle.forEach(categoria => {
      if (y > 255) {
        doc.addPage();
        y = margen;
      }

      doc.setFillColor(230, 246, 243);
      doc.roundedRect(margen, y, 180, 9, 2, 2, 'F');
      doc.setTextColor(0, 77, 64);
      doc.setFontSize(9);
      doc.setFont('helvetica', 'bold');
      doc.text(categoria.nombre, margen + 3, y + 6);
      doc.text(`${categoria.promedioEscala10.toFixed(2)}/10`, 190, y + 6, { align: 'right' });
      y += 13;

      categoria.preguntas.forEach(pregunta => {
        if (y > 245) {
          doc.addPage();
          y = margen;
        }

        doc.setTextColor(31, 45, 41);
        doc.setFontSize(8.5);
        doc.setFont('helvetica', 'bold');
        const preguntaTexto = `${pregunta.orden}. ${pregunta.textoPregunta}`;
        const preguntaLineas = doc.splitTextToSize(preguntaTexto, 150);
        doc.text(preguntaLineas, margen, y);
        doc.setTextColor(1, 144, 126);
        doc.text(`${pregunta.promedioEscala10.toFixed(2)}/10`, 190, y, { align: 'right' });
        y += preguntaLineas.length * 4 + 2;

        const rows = pregunta.filas.map(fila => [
          fila.grupo,
          fila.nombreMateria,
          fila.academiaNombre || '-',
          this.tipoLabel(fila.tipoInstrumento || 'Curricular'),
          String(fila.totalEvaluaciones),
          fila.promedioEscala10.toFixed(2),
          fila.distribucionTexto
        ]);

        autoTable(doc, {
          startY: y,
          head: [['Grupo', 'Materia', 'Academia', 'Tipo', 'Eval.', 'Prom.', 'Distribución']],
          body: rows,
          margin: { left: margen, right: margen },
          styles: { fontSize: 6.5, cellPadding: 1.5, overflow: 'linebreak', valign: 'middle' },
          headStyles: { fillColor: [1, 144, 126], textColor: 255, fontStyle: 'bold' },
          alternateRowStyles: { fillColor: [245, 247, 248] },
          columnStyles: {
            0: { cellWidth: 16 },
            1: { cellWidth: 38 },
            2: { cellWidth: 30 },
            3: { cellWidth: 21 },
            4: { cellWidth: 13, halign: 'right' },
            5: { cellWidth: 14, halign: 'right' },
            6: { cellWidth: 48 }
          }
        });

        y = (doc as any).lastAutoTable.finalY + 5;
      });

      y += 2;
    });

    return y;
  }

  private agregarComentariosPdf(
    doc: any,
    reporte: CalificacionMaestro,
    y: number,
    margen: number
  ): number {
    const materiasConComentarios = reporte.porMateria.filter(mat => mat.comentarios.length > 0);
    if (materiasConComentarios.length === 0) return y;

    if (y > 245) {
      doc.addPage();
      y = margen;
    }

    doc.setTextColor(31, 45, 41);
    doc.setFontSize(11);
    doc.setFont('helvetica', 'bold');
    doc.text('COMENTARIOS Y RECOMENDACIONES', margen, y);
    y += 7;

    materiasConComentarios.forEach(mat => {
      if (y > 258) {
        doc.addPage();
        y = margen;
      }

      doc.setFontSize(8.5);
      doc.setFont('helvetica', 'bold');
      doc.setTextColor(0, 77, 64);
      doc.text(`Grupo: ${mat.grupo} · Materia: ${mat.nombreMateria}`, margen, y);
      y += 5;

      doc.setFont('helvetica', 'normal');
      doc.setFontSize(8);
      doc.setTextColor(69, 83, 79);

      mat.comentarios.forEach((comentario, idx) => {
        if (y > 270) {
          doc.addPage();
          y = margen;
        }

        const lineas = doc.splitTextToSize(`${idx + 1}. ${comentario}`, 180);
        doc.text(lineas, margen, y);
        y += lineas.length * 4.2 + 2;
      });

      y += 3;
    });

    return y;
  }

  private construirResumenOpcionesPdf(reporte: CalificacionMaestro): ResumenOpcionPdf[] {
    const opcionesBase = [
      { etiqueta: 'SIEMPRE', aliases: ['siempre'], color: '#008000' },
      { etiqueta: 'CASI SIEMPRE', aliases: ['casi siempre'], color: '#8cc63e' },
      { etiqueta: 'REGULARMENTE', aliases: ['regularmente', 'algunas veces', 'a veces'], color: '#e7e600' },
      { etiqueta: 'POCAS VECES', aliases: ['pocas veces', 'casi nunca'], color: '#f59e0b' },
      { etiqueta: 'NUNCA', aliases: ['nunca'], color: '#ef1c1c' }
    ];

    const acumulado = new Map<string, number>();

    reporte.porMateria.forEach(materia => {
      materia.porPregunta.forEach(pregunta => {
        Object.entries(pregunta.distribucion ?? {}).forEach(([opcion, votos]) => {
          const clave = this.normalizarOpcionPdf(opcion);
          acumulado.set(clave, (acumulado.get(clave) ?? 0) + Number(votos ?? 0));
        });
      });
    });

    const usados = new Set<string>();
    const resumen = opcionesBase.map(opcion => {
      const votos = opcion.aliases.reduce((sum, alias) => {
        const clave = this.normalizarOpcionPdf(alias);
        usados.add(clave);
        return sum + (acumulado.get(clave) ?? 0);
      }, 0);

      return {
        etiqueta: opcion.etiqueta,
        votos,
        porcentaje: 0,
        color: opcion.color
      };
    });

    acumulado.forEach((votos, clave) => {
      if (usados.has(clave)) return;
      resumen.push({
        etiqueta: clave.toUpperCase(),
        votos,
        porcentaje: 0,
        color: '#6b7280'
      });
    });

    const total = resumen.reduce((sum, item) => sum + item.votos, 0);
    return resumen.map(item => ({
      ...item,
      porcentaje: total > 0 ? (item.votos / total) * 100 : 0
    }));
  }

  private construirDetalleCategorias(reporte: CalificacionMaestro): ReporteCategoriaDetalle[] {
    const categorias = new Map<string, {
      nombre: string;
      orden: number;
      suma: number;
      total: number;
      preguntas: Map<string, {
        preguntaId: number;
        orden: number;
        textoPregunta: string;
        suma: number;
        total: number;
        filas: ReportePreguntaFilaDetalle[];
      }>;
    }>();

    reporte.porMateria.forEach(materia => {
      materia.porPregunta.forEach(pregunta => {
        const categoriaNombre = pregunta.categoriaNombre || 'SIN CATEGORÍA';
        const categoriaOrden = pregunta.categoriaOrden || 999;
        const categoriaKey = `${categoriaOrden}|${categoriaNombre}`;
        const preguntaOrden = pregunta.orden || pregunta.preguntaId;
        const preguntaKey = `${preguntaOrden}|${pregunta.textoPregunta}`;
        const totalRespuestas = pregunta.totalRespuestas || this.contarDistribucion(pregunta.distribucion);

        if (!categorias.has(categoriaKey)) {
          categorias.set(categoriaKey, {
            nombre: categoriaNombre,
            orden: categoriaOrden,
            suma: 0,
            total: 0,
            preguntas: new Map()
          });
        }

        const categoria = categorias.get(categoriaKey)!;

        if (!categoria.preguntas.has(preguntaKey)) {
          categoria.preguntas.set(preguntaKey, {
            preguntaId: pregunta.preguntaId,
            orden: preguntaOrden,
            textoPregunta: pregunta.textoPregunta,
            suma: 0,
            total: 0,
            filas: []
          });
        }

        const preguntaDetalle = categoria.preguntas.get(preguntaKey)!;
        categoria.suma += pregunta.promedio * totalRespuestas;
        categoria.total += totalRespuestas;
        preguntaDetalle.suma += pregunta.promedio * totalRespuestas;
        preguntaDetalle.total += totalRespuestas;
        preguntaDetalle.filas.push({
          materiaId: materia.materiaId,
          nombreMateria: materia.nombreMateria,
          grupo: materia.grupo,
          tipoInstrumento: materia.tipoInstrumento,
          academiaNombre: materia.academiaNombre,
          carreraClave: materia.carreraClave,
          totalEvaluaciones: materia.totalEvaluaciones,
          promedio: pregunta.promedio,
          promedioEscala10: this.aEscala10(pregunta.promedio),
          distribucion: pregunta.distribucion,
          distribucionTexto: this.formatearDistribucion(pregunta.distribucion)
        });
      });
    });

    return Array.from(categorias.values())
      .sort((a, b) => a.orden - b.orden || a.nombre.localeCompare(b.nombre))
      .map(categoria => ({
        nombre: categoria.nombre,
        orden: categoria.orden,
        promedio: categoria.total > 0 ? Math.round((categoria.suma / categoria.total) * 100) / 100 : 0,
        promedioEscala10: categoria.total > 0 ? this.aEscala10(categoria.suma / categoria.total) : 0,
        preguntas: Array.from(categoria.preguntas.values())
          .sort((a, b) => a.orden - b.orden || a.textoPregunta.localeCompare(b.textoPregunta))
          .map(pregunta => ({
            preguntaId: pregunta.preguntaId,
            orden: pregunta.orden,
            textoPregunta: pregunta.textoPregunta,
            promedio: pregunta.total > 0 ? Math.round((pregunta.suma / pregunta.total) * 100) / 100 : 0,
            promedioEscala10: pregunta.total > 0 ? this.aEscala10(pregunta.suma / pregunta.total) : 0,
            totalRespuestas: pregunta.total,
            filas: pregunta.filas.sort((a, b) =>
              a.grupo.localeCompare(b.grupo) ||
              a.nombreMateria.localeCompare(b.nombreMateria))
          }))
      }));
  }

  private actualizarResumenVisual(reporte: CalificacionMaestro | null): void {
    if (!reporte) {
      this.resumenVisualReporte = [];
      this.resumenVisualTotalVotos = 0;
      this.resumenVisualGradient = '#e9edf2';
      this.detalleCategoriasReporte = [];
      return;
    }

    const resumen = this.construirResumenOpcionesPdf(reporte);
    this.resumenVisualReporte = resumen;
    this.resumenVisualTotalVotos = resumen.reduce((sum, item) => sum + item.votos, 0);
    this.resumenVisualGradient = this.construirGradienteResumen(resumen);
    this.detalleCategoriasReporte = this.construirDetalleCategorias(reporte);
  }

  private construirGradienteResumen(resumen: ResumenOpcionPdf[]): string {
    const total = resumen.reduce((sum, item) => sum + item.votos, 0);
    if (total <= 0) return '#e9edf2';

    let cursor = 0;
    const segmentos = resumen
      .filter(item => item.votos > 0)
      .map((item, index, items) => {
        const inicio = cursor;
        cursor += (item.votos / total) * 100;
        const fin = index === items.length - 1 ? 100 : cursor;
        return `${item.color} ${inicio.toFixed(2)}% ${fin.toFixed(2)}%`;
      });

    return `conic-gradient(${segmentos.join(', ')})`;
  }

  private crearImagenResumenPdf(resumen: ResumenOpcionPdf[], calificacionTexto: string): string {
    const canvas = document.createElement('canvas');
    canvas.width = 1100;
    canvas.height = 525;

    const ctx = canvas.getContext('2d');
    if (!ctx) return canvas.toDataURL('image/png');

    const totalVotos = resumen.reduce((sum, item) => sum + item.votos, 0);

    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    ctx.strokeStyle = '#263238';
    ctx.lineWidth = 3;
    ctx.strokeRect(8, 8, canvas.width - 16, canvas.height - 16);

    ctx.fillStyle = '#f4f6f9';
    ctx.fillRect(10, 10, canvas.width - 20, 48);
    ctx.fillStyle = '#e0e0e0';
    ctx.fillRect(900, 10, 190, 48);

    ctx.fillStyle = '#000000';
    ctx.font = '700 24px Arial';
    ctx.textAlign = 'center';
    ctx.fillText('RESUMEN VISUAL DE RESULTADOS', canvas.width / 2, 42);
    ctx.font = '20px Arial';
    ctx.fillText('SAED', 995, 42);

    ctx.font = '24px Arial';
    ctx.fillText(calificacionTexto, 300, 112);

    ctx.font = '700 14px Arial';
    ctx.fillText('ESPECTRO DE LA EVALUACIÓN', 410, 145);

    const cx = 410;
    const cy = 330;
    const radius = 165;
    let start = -Math.PI / 2;

    if (totalVotos === 0) {
      ctx.beginPath();
      ctx.arc(cx, cy, radius, 0, Math.PI * 2);
      ctx.fillStyle = '#e9edf2';
      ctx.fill();
    } else {
      resumen.forEach(item => {
        if (item.votos <= 0) return;
        const end = start + (item.votos / totalVotos) * Math.PI * 2;
        ctx.beginPath();
        ctx.moveTo(cx, cy);
        ctx.arc(cx, cy, radius, start, end);
        ctx.closePath();
        ctx.fillStyle = item.color;
        ctx.fill();
        start = end;
      });
    }

    ctx.fillStyle = '#000000';
    ctx.font = '700 14px Arial';
    ctx.textAlign = 'left';
    ctx.fillText('OPCIONES', 680, 145);
    ctx.textAlign = 'center';
    ctx.fillText('VOTOS', 875, 145);
    ctx.fillText('%', 950, 145);

    ctx.font = '13px Arial';
    resumen.forEach((item, index) => {
      const rowY = 175 + index * 45;
      ctx.fillStyle = item.color;
      ctx.fillRect(685, rowY - 15, 22, 22);

      ctx.fillStyle = '#000000';
      ctx.textAlign = 'left';
      ctx.fillText(item.etiqueta, 720, rowY);
      ctx.textAlign = 'center';
      ctx.fillText(String(item.votos), 875, rowY);
      ctx.fillText(`${item.porcentaje.toFixed(item.porcentaje === 0 ? 0 : 2)} %`, 950, rowY);
    });

    return canvas.toDataURL('image/png');
  }

  private contarDistribucion(distribucion: Record<string, number>): number {
    return Object.values(distribucion ?? {}).reduce((sum, value) => sum + Number(value ?? 0), 0);
  }

  private aEscala10(valorEscala5: number): number {
    return Math.round((valorEscala5 / 5) * 10 * 100) / 100;
  }

  private formatearDistribucion(distribucion: Record<string, number>): string {
    const orden = ['Siempre', 'Casi siempre', 'Regularmente', 'Pocas veces', 'Nunca'];
    const usados = new Set<string>();
    const partes: string[] = [];

    orden.forEach(etiqueta => {
      const entrada = Object.entries(distribucion ?? {})
        .find(([key]) => this.normalizarOpcionPdf(key) === this.normalizarOpcionPdf(etiqueta));

      if (entrada) {
        usados.add(entrada[0]);
        partes.push(`${entrada[0]}: ${entrada[1]}`);
      }
    });

    Object.entries(distribucion ?? {}).forEach(([key, value]) => {
      if (!usados.has(key)) partes.push(`${key}: ${value}`);
    });

    return partes.join(' | ');
  }

  private normalizarOpcionPdf(value: string): string {
    return value
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .trim()
      .replace(/\s+/g, ' ')
      .toLowerCase();
  }

  private sanitizarNombre(value: string): string {
    return value
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[<>:"/\\|?*]+/g, '_')
      .replace(/\s+/g, '_')
      .slice(0, 120);
  }

}
