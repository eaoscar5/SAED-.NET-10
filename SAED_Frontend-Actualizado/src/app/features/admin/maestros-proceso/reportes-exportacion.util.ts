export type AlcanceExportacionZip = 'global' | 'materia';

export interface ReporteExportableZip {
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
}

export function obtenerUnidadesExportacionZip<T extends ReporteExportableZip>(
  rows: T[],
  alcance: AlcanceExportacionZip
): T[] {
  const unidades = new Map<string, T>();

  rows.forEach(row => {
    const key = alcance === 'global'
      ? `${row.procesoId}|${row.idMaestro}`
      : [
          row.procesoId,
          row.idMaestro,
          row.materiaId,
          row.grupo,
          row.tipoInstrumento,
          row.instrumentoId ?? 'sin-instrumento'
        ].join('|');

    if (!unidades.has(key)) unidades.set(key, row);
  });

  return Array.from(unidades.values());
}

export function crearRutaReporteZip(
  row: ReporteExportableZip,
  periodo: string,
  alcance: AlcanceExportacionZip
): string {
  const carpetaProceso = sanitizarSegmento(
    `${row.nombreProceso}_${periodo}_${row.anio}`
  );

  if (alcance === 'global') {
    const archivoGlobal = `${sanitizarSegmento(`${row.nombreMaestro}_${row.idMaestro}`)}.pdf`;
    return `${carpetaProceso}/${archivoGlobal}`;
  }

  const carpetaMaestro = sanitizarSegmento(
    `${row.nombreMaestro}_${row.idMaestro}`
  );
  const instrumento = row.instrumentoId == null
    ? row.tipoInstrumento
    : `${row.tipoInstrumento}_instrumento_${row.instrumentoId}`;
  const archivoMateria = sanitizarSegmento(
    `${row.nombreMateria}_${row.materiaId}_grupo_${row.grupo}_${instrumento}`
  );

  return `${carpetaProceso}/${carpetaMaestro}/${archivoMateria}.pdf`;
}

export function reservarRutaZip(ruta: string, usadas: Set<string>): string {
  let candidata = ruta;
  let consecutivo = 2;

  while (usadas.has(candidata.toLocaleLowerCase())) {
    const punto = ruta.lastIndexOf('.');
    candidata = punto > ruta.lastIndexOf('/')
      ? `${ruta.slice(0, punto)}_${consecutivo}${ruta.slice(punto)}`
      : `${ruta}_${consecutivo}`;
    consecutivo++;
  }

  usadas.add(candidata.toLocaleLowerCase());
  return candidata;
}

function sanitizarSegmento(value: string): string {
  const resultado = value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[<>:"/\\|?*]+/g, '_')
    .replace(/\s+/g, '_')
    .replace(/[. ]+$/g, '')
    .slice(0, 120);

  return resultado || 'sin_dato';
}
