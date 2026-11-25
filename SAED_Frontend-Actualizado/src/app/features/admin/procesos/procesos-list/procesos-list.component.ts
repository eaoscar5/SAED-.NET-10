import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ProcesoService } from 'src/app/core/services/proceso.service';
import { ProcesoDto } from '../../dashboard/models/proceso.model'; // ajusta ruta si es necesario

@Component({
    selector: 'app-procesos-list',
    templateUrl: './procesos-list.component.html',
    styleUrl: './procesos-list.component.scss',
    imports: [CommonModule, ReactiveFormsModule, RouterModule]
})
export class ProcesosListComponent implements OnInit {

  procesos:     ProcesoDto[] = [];
  instrumentos: any[]        = [];
  loading   = false;
  guardando = false;
  errorMsg  = '';
  showModal = false;
  formProceso!: FormGroup;

  constructor(
    private procesoService: ProcesoService,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.cargarInstrumentos();
    this.cargar();
  }

  // ── Form ─────────────────────────────────────────────────────────────────
  // Nota: "nombre" eliminado — el backend lo genera automáticamente
  initForm(): void {
    this.formProceso = this.fb.group({
      anio:          ['', [Validators.required, Validators.min(2000)]],
      periodo:       ['', Validators.required],
      fechaInicio:   ['', Validators.required],
      fechaFin:      ['', Validators.required],
      instrumentoIds: this.fb.array([this.crearInstrumentoControl()]),
    });
  }

  get instrumentosFormArray(): FormArray {
    return this.formProceso.get('instrumentoIds') as FormArray;
  }

  // ── Datos ────────────────────────────────────────────────────────────────

  cargar(): void {
    this.loading  = true;
    this.errorMsg = '';
    this.procesoService.getAll().subscribe({
      next:  (res) => { this.procesos = res; this.loading = false; },
      error: ()    => { this.errorMsg = 'No se pudieron cargar los procesos.'; this.loading = false; },
    });
  }

  cargarInstrumentos(): void {
    this.procesoService.getInstrumentos().subscribe({
      next:  (res) => this.instrumentos = res,
      error: ()    => this.instrumentos = [],
    });
  }

  // ── Modal ────────────────────────────────────────────────────────────────

  abrirModal(): void  { this.showModal = true; }
  cerrarModal(): void {
    this.showModal = false;
    this.formProceso.reset();
    this.reiniciarInstrumentos();
    this.errorMsg = '';
  }

  agregarInstrumento(): void {
    if (!this.puedeAgregarInstrumento()) return;
    this.instrumentosFormArray.push(this.crearInstrumentoControl());
  }

  eliminarInstrumento(index: number): void {
    if (this.instrumentosFormArray.length === 1) {
      this.instrumentosFormArray.at(0).reset('');
      this.instrumentosFormArray.at(0).markAsUntouched();
      return;
    }

    this.instrumentosFormArray.removeAt(index);
  }

  puedeAgregarInstrumento(): boolean {
    return this.instrumentosFormArray.length < this.instrumentos.length;
  }

  instrumentoSeleccionadoEnOtroControl(instrumentoId: number, indexActual: number): boolean {
    return this.instrumentosFormArray.controls.some((control, index) =>
      index !== indexActual && Number(control.value) === instrumentoId
    );
  }

  // ── Acciones ─────────────────────────────────────────────────────────────

  crear(): void {
  if (this.formProceso.invalid) {
    this.formProceso.markAllAsTouched();
    this.instrumentosFormArray.controls.forEach(control => control.markAsTouched());
    return;
  }

  this.guardando = true;
  this.errorMsg  = '';

  const raw = this.formProceso.value;
  const instrumentoIds = (raw.instrumentoIds ?? [])
    .map((id: string | number) => Number(id))
    .filter((id: number) => id > 0);

  if (instrumentoIds.length === 0) {
    this.guardando = false;
    this.errorMsg = 'Debes seleccionar al menos un instrumento.';
    return;
  }

  if (new Set(instrumentoIds).size !== instrumentoIds.length) {
    this.guardando = false;
    this.errorMsg = 'No puedes seleccionar el mismo instrumento más de una vez.';
    return;
  }

  // Convertir strings a números
  const payload = {
    anio:          Number(raw.anio),
    periodo:       Number(raw.periodo),
    fechaInicio:   raw.fechaInicio,
    fechaFin:      raw.fechaFin,
    instrumentoId: instrumentoIds[0],
    instrumentoIds,
  };

  this.procesoService.crearProceso(payload).subscribe({
    next: () => {
      this.guardando = false;
      this.cerrarModal();
      this.cargar();
    },
    error: (err) => {
      this.guardando = false;
      this.errorMsg = err?.error?.message ?? 'No se pudo crear el proceso.';
    },
  });
}

  activar(id: number): void {
    if (!confirm('¿Activar este proceso? Se cerrará el proceso activo actual.')) return;
    this.procesoService.activar(id).subscribe({
      next:  () => this.cargar(),
      error: (err) => this.errorMsg = err?.error?.message ?? 'Error al activar.',
    });
  }

  cerrar(id: number): void {
    if (!confirm('¿Cerrar este proceso?')) return;
    this.procesoService.cerrar(id).subscribe({
      next:  () => this.cargar(),
      error: (err) => this.errorMsg = err?.error?.message ?? 'Error al cerrar.',
    });
  }

  // ── Helper ───────────────────────────────────────────────────────────────

  periodoLabel(periodo: number): string {
    const labels: Record<number, string> = {
      1: 'Sep - Dic',
      2: 'Ene - Abr',
      3: 'May - Ago',
    };
    return labels[periodo] ?? String(periodo);
  }

  nombresInstrumentos(proceso: ProcesoDto): string[] {
    if (proceso.nombresInstrumentos?.length) return proceso.nombresInstrumentos;
    return proceso.nombreInstrumento ? [proceso.nombreInstrumento] : [];
  }

  private crearInstrumentoControl() {
    return this.fb.control('', Validators.required);
  }

  private reiniciarInstrumentos(): void {
    this.instrumentosFormArray.clear();
    this.instrumentosFormArray.push(this.crearInstrumentoControl());
  }
}
