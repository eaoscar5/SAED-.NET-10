# SAED - Seguridad fase 1

## Variables de entorno requeridas en producción

Configurar estos valores fuera del código versionado:

```text
ConnectionStrings__DefaultConnection
Jwt__Key
Jwt__Issuer
Jwt__Audience
UniversidadApi__BaseUrl
UniversidadApi__ServiceUsername
UniversidadApi__ServicePassword
UniversidadApi__AdminUsernames__0
SeedAdmin__Enabled
SeedAdmin__Username
SeedAdmin__Password
```

`SeedAdmin__Enabled` debe permanecer en `false` salvo una inicialización controlada. Si se habilita, `SeedAdmin__Username` y `SeedAdmin__Password` deben provenir de secretos o variables de entorno. El seed no permite el usuario administrador predeterminado conocido.

## Rotación de secretos

Los secretos que alguna vez estuvieron versionados deben rotarse manualmente:

- contraseña PostgreSQL;
- clave JWT;
- credenciales de la API escolar;
- contraseña del administrador predeterminado.

Quitar un secreto del archivo actual no lo elimina del historial Git. Revisar historial y credenciales expuestas antes de considerar seguro el entorno.

## Riesgo pendiente documentado

La API escolar continúa configurada con HTTP porque esta fase no cambia su protocolo ni inventa una solución fuera del alcance. Debe mantenerse registrado como riesgo pendiente de infraestructura/integración.

## Diagnóstico antes de aplicar la migración

No ejecutar la migración en producción sin revisar duplicados. La regla nueva identifica una evaluación duplicada como:

```text
ProcesoId + MatriculaAlumno + AsignacionAcademicaId + InstrumentoId
```

donde `AsignacionAcademicaId` se deriva de:

```text
UPPER(TRIM(Grupo)) || '|' || UPPER(TRIM(IdMaestro)) || '|' || UPPER(TRIM(MateriaId))
```

Consulta para detectar duplicados de evaluación antes de crear el índice:

```sql
SELECT
  "ProcesoId",
  "MatriculaAlumno",
  UPPER(TRIM(COALESCE("Grupo", ''))) || '|' ||
  UPPER(TRIM(COALESCE("IdMaestro", ''))) || '|' ||
  UPPER(TRIM(COALESCE("MateriaId", ''))) AS "AsignacionAcademicaId",
  "InstrumentoId",
  COUNT(*) AS total
FROM "Evaluaciones"
WHERE "InstrumentoId" IS NOT NULL
GROUP BY
  "ProcesoId",
  "MatriculaAlumno",
  UPPER(TRIM(COALESCE("Grupo", ''))) || '|' ||
  UPPER(TRIM(COALESCE("IdMaestro", ''))) || '|' ||
  UPPER(TRIM(COALESCE("MateriaId", ''))),
  "InstrumentoId"
HAVING COUNT(*) > 1;
```

Consulta para detectar respuestas duplicadas que bloquearían el índice `EvaluacionId + PreguntaId`:

```sql
SELECT "EvaluacionId", "PreguntaId", COUNT(*) AS total
FROM "Respuestas"
GROUP BY "EvaluacionId", "PreguntaId"
HAVING COUNT(*) > 1;
```

Si cualquiera de estas consultas devuelve filas, no borrar datos automáticamente. Revisar y corregir manualmente con criterio institucional.

## Privacidad pendiente

La fase actual mantiene la evaluación confidencial vinculada a matrícula para prevenir duplicados y dar seguimiento de avance. Queda pendiente decidir si la institución mantendrá este modelo o rediseñará separación entre cumplimiento y contenido de respuestas.
