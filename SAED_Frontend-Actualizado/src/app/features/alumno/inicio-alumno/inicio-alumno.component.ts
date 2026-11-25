import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from 'src/app/core/services/auth.service';
import { ThemeToggleComponent } from 'src/app/shared/theme-toggle/theme-toggle.component';
import {
  EvaluacionService,
  EvaluacionDisponibleDto,
  FinalizacionEvaluacionesDto
} from '../../../core/services/evaluacion.service';

@Component({
    selector: 'app-inicio-alumno',
    templateUrl: './inicio-alumno.component.html',
    styleUrl: './inicio-alumno.component.scss',
    imports: [CommonModule, RouterModule, FormsModule, ThemeToggleComponent]
})
export class InicioAlumnoComponent implements OnInit {

  evaluaciones:          EvaluacionDisponibleDto[] = [];
  evaluacionesFiltradas: EvaluacionDisponibleDto[] = [];
  loading    = false;
  errorMsg   = '';
  busqueda   = '';
  mostrarModal = false;
  finalizacion: FinalizacionEvaluacionesDto | null = null;
  cargandoFinalizacion = false;

  sesion = this.authService.getSesion();

  constructor(
    private evaluacionService: EvaluacionService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.mostrarModal = this.authService.consumirBienvenidaAlumno();
    this.cargar();
  }

  cargar(): void {
    this.loading  = true;
    this.errorMsg = '';
    this.finalizacion = null;

    this.evaluacionService.getDisponibles().subscribe({
      next: (data) => {
        this.evaluaciones          = data;
        this.evaluacionesFiltradas = data;
        this.loading = false;

        if (this.todasCompletadas)
          this.cargarFinalizacion();
      },
      error: (err) => {
        this.errorMsg = err?.error?.message ?? 'No se pudieron cargar las evaluaciones.';
        this.loading  = false;
      },
    });
  }

  private cargarFinalizacion(): void {
    this.cargandoFinalizacion = true;
    this.finalizacion = null;

    this.evaluacionService.getFinalizacionActual().subscribe({
      next: (finalizacion) => {
        this.finalizacion = finalizacion.completada ? finalizacion : null;
        this.cargandoFinalizacion = false;
      },
      error: () => {
        this.cargandoFinalizacion = false;
      }
    });
  }

  filtrar(): void {
    const q = this.busqueda.toLowerCase().trim();
    this.evaluacionesFiltradas = q
      ? this.evaluaciones.filter(e =>
          e.nombreMaestro.toLowerCase().includes(q) ||
          e.materia.toLowerCase().includes(q)
        )
      : [...this.evaluaciones];
  }

  irAEvaluar(evaluacion: EvaluacionDisponibleDto): void {
    if (evaluacion.yaEvaluado) return;
    this.router.navigate(['evaluar'], { state: { evaluacion } });
  }

  cerrarModal(): void {
    this.mostrarModal = false;
  }

  logout(): void {
    this.authService.logout();
  }

  get totalEvaluaciones(): number { return this.evaluaciones.length; }
  get totalEvaluadas(): number    { return this.evaluaciones.filter(e => e.yaEvaluado).length; }
  get todasCompletadas(): boolean { return this.totalEvaluaciones > 0 && this.totalEvaluadas === this.totalEvaluaciones; }
}
