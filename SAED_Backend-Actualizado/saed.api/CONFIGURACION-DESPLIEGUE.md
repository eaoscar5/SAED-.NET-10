# Configuracion de credenciales y despliegue

## Desarrollo local

Para probar la sincronizacion real en una PC local, editar:

`saed.api/appsettings.Development.json`

Colocar ahi las credenciales reales de la API escolar:

```json
{
  "UniversidadApi": {
    "BaseUrl": "http://siuts.uts.edu.mx/apienlinea/api",
    "ServiceUsername": "<usuario_api_escolar>",
    "ServicePassword": "<password_api_escolar>",
    "ExtracurricularAcademiaClave": "ACD",
    "ServiceTokenSeconds": 25,
    "AdminUsernames": [
      "<usuario_admin_api_escolar>"
    ]
  }
}
```

Ese archivo esta ignorado por Git en este proyecto, para evitar subir credenciales reales.

## Produccion / Somee

No guardar credenciales reales en el repositorio. Usar una de estas opciones:

1. Variables de entorno del servidor:

```text
UniversidadApi__ServiceUsername
UniversidadApi__ServicePassword
UniversidadApi__AdminUsernames__0
Jwt__Key
Jwt__Issuer
Jwt__Audience
ConnectionStrings__DefaultConnection
SeedAdmin__Enabled
SeedAdmin__Username
SeedAdmin__Password
```

2. Si Somee no permite configurar variables de entorno comodamente, copiar `appsettings.Production.example.json` como `appsettings.Production.json` en el servidor y reemplazar los valores placeholder.

El archivo `appsettings.Production.json` real no debe subirse al repositorio.

## Requisitos para probar reportes reales

1. Aplicar migraciones en la base de datos real.
2. Configurar la cadena de conexion real.
3. Configurar `Jwt:Key` con un secreto fuerte.
4. Configurar credenciales reales de `UniversidadApi`.
5. Configurar los usuarios autorizados para login admin externo en `UniversidadApi:AdminUsernames`.
6. Entrar al panel admin y presionar `Sincronizar catalogos`.
7. Crear o tener evaluaciones reales guardadas por estudiantes.

Los catalogos llenan filtros. Las evaluaciones guardadas llenan la tabla de reportes.

## Administrador inicial

El administrador local ya no se crea automaticamente con credenciales predeterminadas conocidas.

Para una inicializacion controlada, configurar temporalmente:

```text
SeedAdmin__Enabled=true
SeedAdmin__Username=<usuario_admin_inicial>
SeedAdmin__Password=<password_secreto>
```

Despues de crear el primer administrador, volver a `SeedAdmin__Enabled=false`.
Si ya existe un usuario con rol `Admin`, el seed no sobrescribe datos.

## Aviso de seguridad

Si una credencial estuvo en archivos versionados, debe rotarse manualmente. Quitarla de `appsettings.json` no la elimina del historial Git.

La API escolar sigue usando HTTP por decision de alcance de esta fase; queda como riesgo pendiente documentado.
