import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiService } from 'src/app/core/services/api.service';
import { AuthService } from 'src/app/core/services/auth.service';
import { ThemeToggleComponent } from 'src/app/shared/theme-toggle/theme-toggle.component';
import {
  EvaluacionService,
  EvaluacionDisponibleDto,
  FinalizacionEvaluacionesDto,
  RespuestaDto
} from 'src/app/core/services/evaluacion.service';

interface Opcion    { id: number; texto: string; valor: number; }
interface Pregunta  { id: number; texto: string; tipo: string | number; orden: number; opciones: Opcion[]; }
interface Categoria { id: number; nombre: string; orden: number; preguntas: Pregunta[]; }
interface Instrumento { id: number; nombre: string; categorias: Categoria[]; }
interface PreguntaTarjeta { categoria: Categoria; pregunta: Pregunta; numero: number; }

@Component({
    selector: 'app-evaluar',
    templateUrl: './evaluar.component.html',
    styleUrl: './evaluar.component.scss',
    imports: [CommonModule, RouterModule, FormsModule, ThemeToggleComponent]
})
export class EvaluarComponent implements OnInit {

  evaluacion: EvaluacionDisponibleDto | null = null;
  instrumento: Instrumento | null = null;

  respuestas:         Record<number, number> = {};
  respuestasAbiertas: Record<number, string> = {};
  comentario    = '';
  intentoEnviar = false;

  loading   = false;
  guardando = false;
  errorMsg  = '';
  enviado   = false;
  evaluacionId: number | null = null;
  finalizacion: FinalizacionEvaluacionesDto | null = null;
  preguntaActualIndex = 0;
  private readonly evaluacionEnCursoKey = 'saed_evaluacion_en_curso';
  private readonly matriculaAlumno?: string;

  constructor(
    private router: Router,
    private api: ApiService,
    authService: AuthService,
    private evaluacionService: EvaluacionService
  ) {
    const sesion = authService.getSesion();
    this.matriculaAlumno = sesion?.rol === 'Alumno' ? sesion.username : undefined;
    const nav = this.router.getCurrentNavigation();
    this.evaluacion = nav?.extras?.state?.['evaluacion'] ?? this.obtenerEvaluacionEnCurso();

    if (this.evaluacion)
      this.guardarEvaluacionEnCurso(this.evaluacion);
  }

  ngOnInit(): void {
    if (!this.evaluacion) {
      this.router.navigate(['/inicio-alumno']);
      return;
    }
    this.cargarInstrumento();
  }

  cargarInstrumento(): void {
    this.loading  = true;
    this.errorMsg = '';

    const instrumentoId = this.evaluacion?.instrumentoId;

    if (!instrumentoId) {
      this.errorMsg = 'No hay instrumento asignado para esta materia.';
      this.loading  = false;
      return;
    }

    this.api.get<any>(`/instrumentos/${instrumentoId}`).subscribe({
      next: (raw) => {
        this.instrumento = {
          id:     raw.id,
          nombre: raw.nombre,
          categorias: (raw.categorias ?? []).map((cat: any) => ({
            id:        cat.id,
            nombre:    cat.nombre ?? '',
            orden:     cat.orden,
            preguntas: (cat.preguntas ?? []).map((p: any) => ({
              id:       p.id,
              texto:    p.texto,
              tipo:     p.tipo,
              orden:    p.orden,
              opciones: (p.opciones ?? [])
                .slice()
                .sort((a: any, b: any) => a.valor - b.valor)
                .map((op: any) => ({
                  id:    op.id,
                  texto: op.texto,
                  valor: op.valor
                }))
            }))
          }))
        };
        this.preguntaActualIndex = 0;
        this.loading = false;
      },
      error: () => { this.errorMsg = 'No se pudo cargar el instrumento.'; this.loading = false; }
    });
  }

  esCerrada(tipo: string | number): boolean {
    return tipo === 1 || tipo === 'Cerrada' || tipo === 'cerrada';
  }

  esAbierta(tipo: string | number): boolean {
    return tipo === 2 || tipo === 'Abierta' || tipo === 'abierta';
  }

  seleccionar(preguntaId: number, opcionId: number): void {
    this.respuestas[preguntaId] = opcionId;
    if (this.errorMsg.startsWith('Debes responder')) {
      this.errorMsg = '';
    }
  }

  opcionSeleccionada(preguntaId: number, opcionId: number): boolean {
    return this.respuestas[preguntaId] === opcionId;
  }

  get tarjetasPreguntas(): PreguntaTarjeta[] {
    const tarjetas: PreguntaTarjeta[] = [];
    for (const categoria of this.instrumento?.categorias ?? []) {
      for (const pregunta of categoria.preguntas ?? []) {
        tarjetas.push({ categoria, pregunta, numero: tarjetas.length + 1 });
      }
    }
    return tarjetas;
  }

  get tarjetaActual(): PreguntaTarjeta | null {
    return this.tarjetasPreguntas[this.preguntaActualIndex] ?? null;
  }

  get preguntaActual(): Pregunta | null {
    return this.tarjetaActual?.pregunta ?? null;
  }

  get totalTarjetas(): number {
    return this.tarjetasPreguntas.length;
  }

  get enComentario(): boolean {
    return this.instrumento !== null && this.preguntaActualIndex >= this.totalTarjetas;
  }

  get puedeRetroceder(): boolean {
    return this.preguntaActualIndex > 0;
  }

  get puedeAvanzar(): boolean {
    const pregunta = this.preguntaActual;
    if (!pregunta || this.enComentario) return false;
    return this.preguntaContestada(pregunta);
  }

  get preguntasRequeridas(): Pregunta[] {
    return this.instrumento?.categorias
      .flatMap(c => c.preguntas)
      .filter(p => this.esCerrada(p.tipo) || this.esAbierta(p.tipo)) ?? [];
  }

  get totalPreguntas(): number   { return this.preguntasRequeridas.length; }
  get totalRespondidas(): number {
    return this.preguntasRequeridas.filter(p => this.preguntaContestada(p)).length;
  }
  get progreso(): number {
    if (this.totalPreguntas === 0) return 0;
    return Math.round((this.totalRespondidas / this.totalPreguntas) * 100);
  }

  get preguntasCompletas(): boolean { return this.totalRespondidas >= this.totalPreguntas && this.totalPreguntas > 0; }
  get comentarioValido(): boolean   { return this.comentario.trim().length > 0; }

  preguntaContestada(pregunta: Pregunta): boolean {
    if (this.esCerrada(pregunta.tipo)) {
      return pregunta.opciones?.length > 0 && this.respuestas[pregunta.id] !== undefined;
    }

    if (this.esAbierta(pregunta.tipo)) {
      return (this.respuestasAbiertas[pregunta.id] ?? '').trim().length > 0;
    }

    return false;
  }

  siguiente(): void {
    const pregunta = this.preguntaActual;
    if (!pregunta || this.enComentario) return;

    if (!this.preguntaContestada(pregunta)) {
      this.intentoEnviar = true;
      return;
    }

    if (this.preguntaActualIndex < this.totalTarjetas) {
      this.preguntaActualIndex++;
      this.errorMsg = '';
    }
  }

  anterior(): void {
    if (!this.puedeRetroceder) return;
    this.preguntaActualIndex--;
    if (this.errorMsg.startsWith('Debes responder') || this.errorMsg.startsWith('El comentario')) {
      this.errorMsg = '';
    }
  }

  opcionClase(opcion: Opcion, index: number): string {
    const texto = this.normalizar(opcion.texto);
    if (texto.includes('casi siempre')) return 'casi-siempre';
    if (texto.includes('casi nunca')) return 'casi-nunca';
    if (texto.includes('a veces')) return 'a-veces';
    if (texto.includes('siempre')) return 'siempre';
    if (texto.includes('nunca')) return 'nunca';

    const clases = ['nunca', 'casi-nunca', 'a-veces', 'casi-siempre', 'siempre'];
    return clases[Math.min(index, clases.length - 1)];
  }

  opcionEmoji(opcion: Opcion, index: number): string {
    switch (this.opcionClase(opcion, index)) {
      case 'nunca': return '😭';
      case 'casi-nunca': return '😞';
      case 'a-veces': return '😐';
      case 'casi-siempre': return '😄';
      default: return '😁';
    }
  }

  enviar(): void {
    this.intentoEnviar = true;

    if (!this.preguntasCompletas) {
      this.errorMsg = `Debes responder todas las preguntas (${this.totalRespondidas}/${this.totalPreguntas}).`;
      this.moverAPrimeraPendiente();
      return;
    }
    if (!this.comentarioValido) {
      this.errorMsg = 'El comentario es obligatorio.';
      this.preguntaActualIndex = this.totalTarjetas;
      return;
    }
    if (!this.evaluacion) return;

    this.guardando = true;
    this.errorMsg  = '';

    const respuestasDto: RespuestaDto[] = this.preguntasRequeridas.map((pregunta) => {
      if (this.esCerrada(pregunta.tipo)) {
        return {
          preguntaId: pregunta.id,
          opcionId: Number(this.respuestas[pregunta.id]),
          texto: null
        };
      }

      return {
        preguntaId: pregunta.id,
        opcionId: null,
        texto: (this.respuestasAbiertas[pregunta.id] ?? '').trim()
      };
    });

    this.evaluacionService.crearEvaluacion({
      asignacionAcademicaId: this.evaluacion.asignacionAcademicaId,
      instrumentoId:         this.evaluacion.instrumentoId!,
      respuestas:            respuestasDto,
      comentario:            this.comentario.trim(),
    }).subscribe({
      next: (res) => {
        this.guardando = false;
        this.enviado = true;
        this.evaluacionId = res.id;
        this.finalizacion = res.finalizacion ?? null;
        this.limpiarEvaluacionEnCurso();
      },
      error: (err) => {
        this.guardando = false;
        this.errorMsg = err?.error?.message ?? err?.message ?? 'Error al enviar.';
      },
    });
  }

  volver(): void {
    this.limpiarEvaluacionEnCurso();
    this.router.navigate(['/inicio-alumno']);
  }

  private guardarEvaluacionEnCurso(evaluacion: EvaluacionDisponibleDto): void {
    if (!this.matriculaAlumno) return;

    try {
      sessionStorage.setItem(this.evaluacionEnCursoKey, JSON.stringify({
        matricula: this.matriculaAlumno,
        evaluacion
      }));
    } catch {
      // Si sessionStorage falla, la evaluacion seguira viva mientras no se recargue la pestana.
    }
  }

  private obtenerEvaluacionEnCurso(): EvaluacionDisponibleDto | null {
    if (!this.matriculaAlumno) return null;

    try {
      const raw = sessionStorage.getItem(this.evaluacionEnCursoKey);
      const guardada = raw ? JSON.parse(raw) as {
        matricula: string;
        evaluacion: EvaluacionDisponibleDto;
      } | null : null;
      return guardada?.matricula === this.matriculaAlumno ? guardada.evaluacion ?? null : null;
    } catch {
      return null;
    }
  }

  private limpiarEvaluacionEnCurso(): void {
    try {
      sessionStorage.removeItem(this.evaluacionEnCursoKey);
    } catch {
      // Nada que limpiar si sessionStorage no esta disponible.
    }
  }

  private moverAPrimeraPendiente(): void {
    const pendienteIndex = this.tarjetasPreguntas.findIndex(({ pregunta }) =>
      !this.preguntaContestada(pregunta)
    );
    if (pendienteIndex >= 0) {
      this.preguntaActualIndex = pendienteIndex;
    }
  }

  private normalizar(texto: string): string {
    return (texto ?? '')
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .trim();
  }
}
