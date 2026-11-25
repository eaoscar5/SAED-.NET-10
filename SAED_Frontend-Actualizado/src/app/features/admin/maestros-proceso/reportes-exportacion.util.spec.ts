import {
  crearRutaReporteZip,
  obtenerUnidadesExportacionZip,
  ReporteExportableZip,
  reservarRutaZip
} from './reportes-exportacion.util';

describe('utilidades de exportación ZIP', () => {
  const base: ReporteExportableZip = {
    procesoId: 7,
    nombreProceso: 'Evaluación 2026',
    anio: 2026,
    periodo: 2,
    idMaestro: 'DOC-1',
    nombreMaestro: 'María López',
    materiaId: 'MAT-1',
    nombreMateria: 'Programación',
    grupo: '4-A',
    tipoInstrumento: 'Curricular',
    instrumentoId: 10
  };

  it('conserva una sola unidad global por maestro y proceso', () => {
    const rows = [
      base,
      { ...base, materiaId: 'MAT-2', nombreMateria: 'Bases de datos' }
    ];

    expect(obtenerUnidadesExportacionZip(rows, 'global')).toEqual([base]);
  });

  it('separa materia, grupo, tipo e instrumento en la exportación individual', () => {
    const rows = [
      base,
      { ...base, materiaId: 'MAT-2' },
      { ...base, grupo: '4-B' },
      { ...base, tipoInstrumento: 'Extracurricular' as const, instrumentoId: 11 }
    ];

    expect(obtenerUnidadesExportacionZip(rows, 'materia')).toHaveSize(4);
  });

  it('genera rutas legibles y evita sobrescribir archivos repetidos', () => {
    const ruta = crearRutaReporteZip(base, 'Ene-Abr', 'materia');
    const usadas = new Set<string>();

    expect(ruta).toContain('Maria_Lopez_DOC-1');
    expect(ruta).toContain('Programacion_MAT-1_grupo_4-A_Curricular_instrumento_10.pdf');
    expect(reservarRutaZip(ruta, usadas)).toBe(ruta);
    expect(reservarRutaZip(ruta, usadas)).toContain('_2.pdf');
  });
});
