import { Injectable } from '@angular/core';
import { ApiService } from '../services/api.service';

export interface EvaluacionDisponibleDto {
  asignacionAcademicaId: string;
  idMaestro:     string;
  nombreMaestro: string;
  materiaId:     string;
  materia:       string;
  grupo:         string;
  instrumentoId?: number | null;
  tipoInstrumento?: 'Curricular' | 'Extracurricular' | number;
  academiaClave?: string | null;
  academiaNombre?: string | null;
  carreraClave?: string | null;
  tipoMateria?: number | null;
  materiaNoCurricular?: boolean;
  yaEvaluado:    boolean;
}

export interface RespuestaDto {
  preguntaId: number;
  opcionId?:  number | null;
  texto?:     string | null;
}

export interface CrearEvaluacionDto {
  asignacionAcademicaId: string;
  instrumentoId:         number;
  respuestas:            RespuestaDto[];
  comentario?:           string;
}

export interface EvaluacionCreadaDto {
  id:           number;
  fecha:        string;
  finalizacion: FinalizacionEvaluacionesDto;
}

export interface FinalizacionEvaluacionesDto {
  completada: boolean;
  folio?: string | null;
  fechaFinalizacion?: string | null;
  totalEvaluaciones: number;
  evaluacionesCompletadas: number;
  grupo: string;
  nombreProceso: string;
  anio: number;
  periodo: number;
}

@Injectable({ providedIn: 'root' })
export class EvaluacionService {
  constructor(private api: ApiService) {}

  getDisponibles() {
    return this.api.get<EvaluacionDisponibleDto[]>('/evaluaciones/disponibles');
  }

  crearEvaluacion(dto: CrearEvaluacionDto) {
    return this.api.post<EvaluacionCreadaDto>('/evaluaciones', dto);
  }

  getFinalizacionActual() {
    return this.api.get<FinalizacionEvaluacionesDto>('/evaluaciones/finalizacion');
  }
}
