# TaskFlow — Entorno de desarrollo con Aspire

> **Responsables:** conjunto — backend y frontend
>
> **Estado:** guía de la implementación actual de la base del backend y del AppHost. La configuración completa de identidad y el modelo de negocio se implementan en sus respectivas issues.
>
> **Decisión:** Aspire orquesta los servicios durante el desarrollo; las migraciones se aplican explícitamente en un paso separado.
>
> **Referencias:** [backend](../03-architecture/backend.md) · [autenticación](../03-architecture/authentication.md) · [modelo de datos](../03-architecture/database.md) · [issue de la base del backend](../issues/ISS-02-backend-base.md).

## 1. Qué se guarda en cada carpeta

- `infrastructure/TaskFlow.AppHost/`: proyecto C# que describe los recursos y su arranque con Aspire.
- `backend/src/`: proyectos `TaskFlow.Domain`, `TaskFlow.Application`, `TaskFlow.Infrastructure` y `TaskFlow.Api`.
- `backend/tests/TaskFlow.Api.IntegrationTests/`: fixture PostgreSQL, factoría de la API y pruebas de salud.
- `front/`: aplicación React/Vite.
- `docs/04-development/`: instrucciones de preparación, ejecución y comprobación.
- `TaskFlow.sln`: solución que agrupa los cuatro proyectos del backend y el proyecto de pruebas. Actualmente no incluye AppHost.
- `dotnet-tools.json`: manifiesto de herramientas locales, con `dotnet-ef` fijado en `10.0.13`.

La solución permite compilar y ejecutar las pruebas del backend en conjunto. AppHost orquesta procesos y contenedores; no es una capa de negocio.

## 2. Recursos y direcciones locales

| Recurso | Ejecución | Dirección o función |
|---|---|---|
| AppHost | Proceso C# sobre .NET 10 | Arranque, dependencias y dashboard; consultar la URL que imprime `aspire run`. |
| API ASP.NET Core | Proceso del equipo, perfil `http` | `http://localhost:5080`; salud en `/health`; OpenAPI en `/openapi/v1.json` en `Development`. |
| SPA React/Vite | Proceso del equipo | `http://localhost:5173`. |
| Keycloak | Contenedor | `http://localhost:8080`. |
| PostgreSQL | Contenedor con volumen persistente | Puerto local `5432`; bases `TaskFlowDb` y `KeyCloakDb`. |
| pgAdmin | Contenedor auxiliar | Abrir el endpoint publicado en el dashboard de la ejecución actual; su puerto no está fijado en AppHost. |

El flujo local usa HTTP. AppHost selecciona el perfil `http` de la API y Keycloak se configura con `WithoutHttpsCertificate()`. La API solo activa la redirección a HTTPS fuera de `Development`. Este procedimiento no requiere instalar ni confiar certificados locales; la configuración del despliegue se definirá por separado.

La preparación del realm `taskflow`, clientes OIDC, roles de aplicación, callback y CORS pertenece a la integración de identidad. Declarar el recurso Keycloak no acredita que esos elementos estén configurados ni que la API valide tokens.

## 3. Preparación y arranque

Se necesitan el SDK .NET 10 compatible con los proyectos, Aspire CLI, Docker en ejecución y Node.js/npm disponibles en `PATH`. Comprobar desde la terminal que se usará para arrancar Aspire:

```powershell
dotnet --version
aspire --version
docker version
node --version
npm --version
```

Desde la raíz del repositorio, restaurar la herramienta local y compilar el backend:

```powershell
dotnet tool restore
dotnet build TaskFlow.sln
```

Arrancar AppHost en esa terminal:

```powershell
Set-Location infrastructure/TaskFlow.AppHost
aspire run
```

El comando permanece en ejecución. Abrir el dashboard indicado y revisar los recursos. AppHost configura la espera de las bases para Keycloak/API y la dependencia del frontend respecto a la API. El recurso `front-installer` instala las dependencias de npm antes de ejecutar Vite; si falla, consultar primero su log. Comprobar que existe `front/package.json` y que Node/npm funcionan desde la terminal de arranque.

Los puertos `5432`, `8080`, `5080` y `5173` deben estar disponibles. Evitar arrancar otra instancia del entorno sobre esos mismos puertos.

El AppHost actual usa la integración de Keycloak y su extensión de PostgreSQL, con paquetes preliminares. Tampoco fija explícitamente las etiquetas de las imágenes PostgreSQL y Keycloak. Por tanto, esta configuración todavía difiere del objetivo de paquetes estables e imágenes fijadas de `backend.md` §2; esa revisión corresponde a la issue del AppHost. La imagen de PostgreSQL de las pruebas sí está fijada en `18.6`.

## 4. Bases de datos, configuración y secretos

Por indicación del responsable para esta primera práctica, ambas bases usan el superusuario predeterminado `postgres`. No se crean roles PostgreSQL separados ni un script inicializador de permisos. Es una simplificación del entorno local respecto al diseño inicial; los roles `Admin` y `User` de la aplicación siguen siendo parte de la integración de identidad pendiente.

AppHost inyecta en la API la conexión de negocio `ConnectionStrings__TaskFlowDb`. La API la lee como `GetConnectionString("TaskFlowDb")` y detiene el arranque si falta. Keycloak utiliza su propia base `KeyCloakDb`; la API se referencia al servicio Keycloak, pero no accede a sus tablas internas.

Los secretos se conservan fuera de Git. La contraseña de PostgreSQL puede consultarse mediante la configuración local de Aspire; para utilizar EF es preferible copiar la conexión completa del recurso `api` desde el dashboard. Una contraseña almacenada en secretos locales puede conservarse entre reinicios; comprobar siempre la conexión de la ejecución actual si cambia la configuración.

| Configuración actual de API | Procedencia |
|---|---|
| `ConnectionStrings__TaskFlowDb` | Conexión de negocio suministrada por AppHost; configurada explícitamente en la terminal que ejecuta EF. |
| `ASPNETCORE_ENVIRONMENT` | `Development`, definido en el perfil local `http`. |

Las opciones de JWT, introspección, CORS y las variables públicas de Vite se incorporarán cuando se implemente esa integración. Las variables de Vite son visibles en el navegador y no deben contener contraseñas ni secretos de cliente.

## 5. Migraciones explícitas, separadas del arranque

Mantener Aspire y PostgreSQL en ejecución. Abrir **otra terminal PowerShell en la raíz del repositorio** y restaurar la herramienta local:

```powershell
dotnet tool restore
```

En el dashboard de Aspire, abrir el recurso `api` y copiar el valor de `ConnectionStrings__TaskFlowDb`. Introducirlo en la nueva terminal:

```powershell
$env:ConnectionStrings__TaskFlowDb = Read-Host "Pega la conexión de TaskFlowDb del recurso api"
```

Comprobar que el valor contiene `Database=TaskFlowDb`, no `Database=postgres` ni `Database=KeyCloakDb`. No guardar la conexión real en este documento ni en archivos versionados. La variable pertenece a esa terminal y a sus procesos hijos: no se crea un archivo `.env` ni se comparte con el resto del equipo. Se pierde al cerrar la terminal; cada desarrollador utiliza su propia conexión local.

Comprobar el contexto y consultar las migraciones:

```powershell
dotnet ef dbcontext info --project backend/src/TaskFlow.Infrastructure --startup-project backend/src/TaskFlow.Api --context TaskFlowDbContext
dotnet ef migrations list --project backend/src/TaskFlow.Infrastructure --startup-project backend/src/TaskFlow.Api --context TaskFlowDbContext
```

El primer comando debe identificar `TaskFlow.Infrastructure.Persistence.TaskFlowDbContext`, proveedor `Npgsql.EntityFrameworkCore.PostgreSQL`, base `TaskFlowDb` y servidor local en el puerto `5432`. Mostrar la información del contexto no sustituye la comprobación real de conectividad de `/health`.

Actualmente el contexto no tiene entidades mapeadas ni migraciones: `No migrations were found` es coherente con ese estado. En una base sin historial, la consulta de `__EFMigrationsHistory` puede registrar el fallo por tabla inexistente antes de mostrar ese resultado. Otros errores de conexión, credenciales o SQL deben investigarse; no crear manualmente la tabla de historial.

**Comando único para aplicar las migraciones disponibles:**

```powershell
dotnet ef database update --project backend/src/TaskFlow.Infrastructure --startup-project backend/src/TaskFlow.Api --context TaskFlowDbContext
```

Se ejecuta al preparar una base vacía o al recibir nuevas migraciones, con la conexión configurada y PostgreSQL disponible. Actualiza el esquema de esa base y aplica solo las migraciones pendientes. Reiniciar Aspire no sustituye este paso ni requiere generar una migración nueva.

La primera migración del modelo de negocio corresponde a [ISS-05 / GitHub #7](../issues/ISS-05-domain-model.md). Cuando se implementen y revisen las entidades y sus mapeos, se podrá crear con:

```powershell
dotnet ef migrations add InitialCreate --project backend/src/TaskFlow.Infrastructure --startup-project backend/src/TaskFlow.Api --context TaskFlowDbContext --output-dir Persistence/Migrations
```

Este último comando genera archivos para revisar y versionar; `database update` los aplica. No generar una migración vacía para completar la issue de la base del backend. Crear o editar datos no requiere migraciones si no cambia el esquema.

La API y AppHost no ejecutan `Database.Migrate`, `MigrateAsync` ni `EnsureCreated` al arrancar. La preparación futura del esquema de pruebas se hará en la infraestructura de pruebas, cuando existan las migraciones. El volumen local conserva los datos entre reinicios; no se debe eliminar para aplicar una actualización ordinaria.

## 6. Verificación de la base del backend

Desde la raíz del repositorio y con Docker en ejecución:

```powershell
dotnet build TaskFlow.sln
dotnet test TaskFlow.sln
```

Para ejecutar únicamente las pruebas de salud:

```powershell
dotnet test TaskFlow.sln --filter "FullyQualifiedName~HealthTest"
```

El nombre de la clase es `HealthTest`, en singular. Verificar que el resumen indica pruebas ejecutadas, no cero coincidencias.

Las dos pruebas actuales usan `WebApplicationFactory` y una fixture con la imagen `postgres:18.6`. Una comprueba `200` y cuerpo `Healthy`; la otra detiene temporalmente su contenedor y comprueba `503`, `application/problem+json` y `status: 503`. La fixture crea una base temporal `taskflow_test`, aislada de los datos locales. No se necesita arrancar Aspire para ejecutar estas pruebas.

Con Aspire arrancado, también puede comprobarse desde Bruno:

| Petición | Resultado esperado actual |
|---|---|
| `GET http://localhost:5080/health` | `200` y `Healthy` con PostgreSQL disponible; `503` y Problem Details si deja de estar disponible manteniendo la API en ejecución. |
| `GET http://localhost:5080/openapi/v1.json` | `200` y documento OpenAPI en `Development`. No implica disponer de una interfaz Swagger. |
| `GET http://localhost:5080/api/v1/ping` | `200`; controlador provisional para comprobar Controllers y el prefijo de ruta. |
| `GET http://localhost:5080/ruta-inexistente`, con `Accept: application/json` | `404` con Problem Details. |

`Domain` no tiene referencias a ASP.NET Core, EF Core ni a las otras capas. `Application` referencia `Domain`; `Infrastructure`, `Application` y `Domain`; `Api`, `Application` e `Infrastructure`.

La base técnica de ISS-02 no incluye todavía los endpoints de negocio completos, JWT, servicios de usuario, relaciones del dominio ni la migración inicial. `ProjectController` y `TaskController` son archivos de preparación sin endpoints implementados; `Ping` y `WeatherForecast` siguen siendo ejemplos provisionales. Los nuevos controladores de negocio deben declarar rutas bajo `/api/v1`; el ejemplo `WeatherForecast` aún conserva su ruta de plantilla.

Los recorridos de login, revocación, `/me`, permisos, datos de demostración y reconstrucción del esquema se verificarán en sus issues. El despliegue AWS continúa pendiente.

## 7. Referencias oficiales

- [Prerequisitos de Aspire y AppHost C#](https://aspire.dev/get-started/prerequisites/?apphost=csharp).
- [Integración de JavaScript y Vite](https://aspire.dev/integrations/frameworks/javascript/).
- [PostgreSQL: recursos y volúmenes](https://aspire.dev/integrations/databases/postgres/postgres-host/).
- [Keycloak: integración y configuración](https://aspire.dev/integrations/security/keycloak/).
- [EF Core: herramienta de línea de comandos](https://learn.microsoft.com/en-us/ef/core/cli/dotnet).
- [EF Core: aplicación de migraciones](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying).
- [Pruebas de integración de ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0).
- [Testcontainers para .NET: PostgreSQL](https://dotnet.testcontainers.org/modules/postgres/).
