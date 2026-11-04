# TaskFlow — Entorno de desarrollo con Aspire

> **Responsables:** conjunto — backend y frontend
>
> **Estado:** guía del backend y AppHost actuales, incluido el modelo persistido y la autenticación de la API. La integración real de Keycloak con la SPA y los endpoints de proyectos/tareas siguen pendientes.
>
> **Decisión:** Aspire orquesta los servicios durante el desarrollo; las migraciones se aplican explícitamente en un paso separado.
>
> **Referencias:** [backend](../03-architecture/backend.md) · [autenticación](../03-architecture/authentication.md) · [modelo de datos](../03-architecture/database.md) · [issue de la base del backend](../issues/ISS-02-backend-base.md).

## 1. Qué se guarda en cada carpeta

- `infrastructure/TaskFlow.AppHost/`: proyecto C# que describe los recursos y su arranque con Aspire.
- `backend/src/`: proyectos `TaskFlow.Domain`, `TaskFlow.Application`, `TaskFlow.Infrastructure` y `TaskFlow.Api`.
- `backend/tests/TaskFlow.Api.IntegrationTests/`: fixture PostgreSQL, factorías de la API y pruebas de salud, esquema y autenticación.
- `backend/tests/TaskFlow.Domain.Test/`: pruebas unitarias del dominio, sin Docker.
- `front/`: aplicación React/Vite.
- `docs/04-development/`: instrucciones de preparación, ejecución y comprobación.
- `TaskFlow.sln`: solución que agrupa los cuatro proyectos del backend y sus dos proyectos de pruebas. Actualmente no incluye AppHost.
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

Por indicación del responsable para esta primera práctica, ambas bases usan el superusuario predeterminado `postgres`. No se crean roles PostgreSQL separados ni un script inicializador de permisos. Es una simplificación del entorno local respecto al diseño inicial; los roles `Admin` y `User` de la aplicación se comprueban separadamente desde el token validado.

AppHost inyecta en la API la conexión de negocio `ConnectionStrings__TaskFlowDb`. La API la lee como `GetConnectionString("TaskFlowDb")` y detiene el arranque si falta. Keycloak utiliza su propia base `KeyCloakDb`; la API se referencia al servicio Keycloak, pero no accede a sus tablas internas.

Los secretos se conservan fuera de Git. La contraseña de PostgreSQL puede consultarse mediante la configuración local de Aspire; para utilizar EF es preferible copiar la conexión completa del recurso `api` desde el dashboard. Una contraseña almacenada en secretos locales puede conservarse entre reinicios; comprobar siempre la conexión de la ejecución actual si cambia la configuración.

| Configuración actual de API | Procedencia |
|---|---|
| `ConnectionStrings__TaskFlowDb` | Conexión de negocio suministrada por AppHost; configurada explícitamente en la terminal que ejecuta EF. |
| `ASPNETCORE_ENVIRONMENT` | `Development`, definido en el perfil local `http`. |
| `Keycloak__Authority` | `http://localhost:8080/realms/taskflow`, configurado en Development y suministrado por AppHost. Debe coincidir con `iss`. |
| `Keycloak__Audience` | `taskflow-api`, en `appsettings.json`; el access token debe incluir esa audiencia. |
| `Keycloak__Introspection__ClientId` | `taskflow-api`, cliente confidencial configurado en el realm. |
| `Keycloak__Introspection__ClientSecret` | AppHost lo inyecta a partir del parámetro secreto existente `taskflow-api-client-secret`; no se versiona. |
| `Keycloak__Introspection__TimeoutSeconds` | 5 segundos por defecto, configurables entre 1 y 30. |
| `Cors__AllowedOrigins__0` | `http://localhost:5173` en Development. Fuera de Development se configura explícitamente junto al emisor HTTPS. |

El secreto del cliente debe coincidir con el del realm importado; la API y Keycloak reciben el mismo parámetro de AppHost. Si se ejecuta la API fuera de Aspire, hay que proporcionar ese secreto mediante configuración local segura. Su ausencia no impide consultar salud, pero una petición con JWT válido recibe 503 porque no se puede comprobar la sesión. Las variables de Vite son visibles en el navegador y no deben contener contraseñas ni secretos de cliente.

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

El contexto ya mapea usuarios, proyectos, pertenencias y tareas y dispone de `InitialCreate`. En una base sin historial, la consulta de `__EFMigrationsHistory` puede registrar el fallo por tabla inexistente antes de listar las migraciones pendientes. Otros errores de conexión, credenciales o SQL deben investigarse; no crear manualmente la tabla de historial.

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

La API y AppHost no ejecutan `Database.Migrate`, `MigrateAsync` ni `EnsureCreated` al arrancar. La fixture de pruebas aplica las migraciones a su contenedor temporal antes de ejecutar las pruebas. El volumen local conserva los datos entre reinicios; no se debe eliminar para aplicar una actualización ordinaria.

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

Las pruebas de salud usan `WebApplicationFactory` y una fixture con la imagen `postgres:18.6`. Una comprueba `200` y cuerpo `Healthy`; la otra detiene temporalmente su contenedor y comprueba `503`, `application/problem+json` y `status: 503`. Las pruebas de esquema y de alta concurrente utilizan contenedores de esa misma versión. La fixture crea una base temporal `taskflow_test`, aislada de los datos locales. No se necesita arrancar Aspire para ejecutar estas pruebas.

Con Aspire arrancado, también puede comprobarse desde Bruno:

| Petición | Resultado esperado actual |
|---|---|
| `GET http://localhost:5080/health` | `200` y `Healthy` con PostgreSQL disponible; `503` y Problem Details si deja de estar disponible manteniendo la API en ejecución. |
| `GET http://localhost:5080/openapi/v1.json` | `200` y documento OpenAPI en `Development`. No implica disponer de una interfaz Swagger. |
| `GET http://localhost:5080/api/v1/me` sin token | `401` y cabecera `WWW-Authenticate: Bearer`. |
| `GET http://localhost:5080/api/v1/me` con access token válido y activo | `200` con perfil local y roles. |
| `GET http://localhost:5080/api/v1/ping` con access token válido y activo | `200`; controlador provisional protegido por la política predeterminada de respaldo. |
| `GET http://localhost:5080/ruta-inexistente`, con token válido, activo y rol admitido | `404` con Problem Details. |

`Domain` no tiene referencias a ASP.NET Core, EF Core ni a las otras capas. `Application` referencia `Domain`; `Infrastructure`, `Application` y `Domain`; `Api`, `Application` e `Infrastructure`.

El modelo de dominio, su migración y la autenticación de la API están implementados. `ProjectController` y `TaskController` siguen sin endpoints de negocio implementados; `Ping` y `WeatherForecast` son ejemplos provisionales protegidos por la política de respaldo. Los nuevos controladores de negocio deben declarar rutas bajo `/api/v1`; el ejemplo `WeatherForecast` aún conserva su ruta de plantilla.

Los permisos por pertenencia, la semilla de usuarios de demostración y el despliegue AWS continúan pendientes.

### 6.1. Autenticación y perfil local

La configuración se concentra en `backend/src/TaskFlow.Api/Authentication/AuthenticationExtensions.cs`. El recorrido es: JWT validado → introspección → roles admitidos → resolución del `AppUser` local → controlador. El identificador que entrega `ICurrentUser.UserId` pertenece a la tabla `app_user`, no es el `sub` de Keycloak. Los usuarios locales se crean automáticamente en su primer acceso autorizado; no se necesita un POST público para crear perfiles ni la API guarda contraseñas.

Para comprobar desde Bruno, arrancar Aspire, aplicar las migraciones pendientes y obtener un **access token** iniciando sesión mediante el frontend. Crear una petición GET a `http://localhost:5080/api/v1/me`, seleccionar autenticación Bearer e introducir el token solo en el entorno local de ejecución. No usar el ID token ni versionar tokens. La respuesta esperada es `{ "id", "displayName", "email", "roles" }`. Repetir sin token (401), con token válido sin `Admin`/`User` (403) y, con un token válido previamente obtenido, sin conexión al endpoint de introspección (503, sin crear ni actualizar el perfil).

Las pruebas HTTP de JWT, roles y errores de introspección no necesitan Docker ni Keycloak:

```powershell
dotnet test backend/tests/TaskFlow.Api.IntegrationTests/TaskFlow.Api.IntegrationTests.csproj --filter "FullyQualifiedName~TaskFlow.Api.IntegrationTests.AuthenticationTests"
```

Estas pruebas mantienen el validador JWT real y solo sustituyen las claves de discovery, la respuesta de introspección y la persistencia. Para comprobar alta concurrente real y salud anónima con PostgreSQL 18.6, se necesita Docker:

```powershell
dotnet test backend/tests/TaskFlow.Api.IntegrationTests/TaskFlow.Api.IntegrationTests.csproj --filter "FullyQualifiedName~AuthenticationDatabaseTests"
```

La aceptación con tokens firmados de prueba no sustituye el recorrido real de login, renovación, logout y revocación administrativa. Ese recorrido se verificará cuando esté integrado el trabajo de frontend/Keycloak.

## 7. Referencias oficiales

- [Prerequisitos de Aspire y AppHost C#](https://aspire.dev/get-started/prerequisites/?apphost=csharp).
- [Integración de JavaScript y Vite](https://aspire.dev/integrations/frameworks/javascript/).
- [PostgreSQL: recursos y volúmenes](https://aspire.dev/integrations/databases/postgres/postgres-host/).
- [Keycloak: integración y configuración](https://aspire.dev/integrations/security/keycloak/).
- [EF Core: herramienta de línea de comandos](https://learn.microsoft.com/en-us/ef/core/cli/dotnet).
- [EF Core: aplicación de migraciones](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying).
- [Pruebas de integración de ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0).
- [Testcontainers para .NET: PostgreSQL](https://dotnet.testcontainers.org/modules/postgres/).
