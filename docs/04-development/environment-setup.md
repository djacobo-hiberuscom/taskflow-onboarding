# TaskFlow — Entorno de desarrollo con Aspire

> **Responsables:** conjunto — backend y frontend
>
> **Estado:** diseño y procedimiento previsto para fase 1. Todavía no existe un AppHost ni una solución ejecutable validada; los comandos con rutas definitivas se añadirán al implementar y comprobar el entorno.
>
> **Decisión:** Aspire orquesta los servicios durante el desarrollo; las migraciones se aplican explícitamente en un paso separado.
>
> **Referencias:** [backend](../03-architecture/backend.md) · [autenticación](../03-architecture/authentication.md) · [modelo de datos](../03-architecture/database.md) · [coherencia y originales pendientes de aprobación](../03-architecture/documentation-alignment.md).

## 1. Qué se guarda en cada carpeta

- `docs/04-development/`: instrucciones para preparar, arrancar, actualizar y comprobar el entorno.
- `infrastructure/TaskFlow.AppHost/`: ubicación prevista del proyecto C# que describe los recursos y su arranque con Aspire.
- `backend/` y `frontend/`: implementación de la API y de la SPA.
- `infrastructure/keycloak/`: configuración de realm prevista, sin secretos ni contraseñas reales.

Estas rutas de implementación se crearán en fase 1. AppHost es configuración ejecutable de orquestación y queda separado de las cuatro capas de negocio. La guía explica cómo utilizarlo.

## 2. Recursos y direcciones acordados

| Recurso | Ejecución local prevista | Dirección pública o función |
|---|---|---|
| AppHost | Proceso C# sobre .NET 10 | Arranque, dependencias, configuración y panel de diagnóstico. |
| API ASP.NET Core | Proceso del equipo iniciado por AppHost | `http://localhost:5080`; salud en `/health`. |
| SPA React/Vite | Proceso del equipo iniciado por AppHost | `http://localhost:5173`. |
| Keycloak | Contenedor iniciado por AppHost | `http://localhost:8080`; realm `taskflow`. |
| PostgreSQL | Contenedor iniciado por AppHost, con volumen persistente | Puerto local 5432; bases lógicas `taskflow` y `keycloak`. |

Se mantendrán fijos los endpoints públicos en el modelo de Aspire, aunque use puertos internos o descubrimiento de servicios. No se copiará una URL interna de contenedor o del dashboard a la configuración del navegador.

El emisor OIDC será `http://localhost:8080/realms/taskflow`; callback `http://localhost:5173/callback`; retorno del logout `http://localhost:5173/`; origen permitido `http://localhost:5173`. La API y el navegador pueden acceder al mismo emisor porque la API se ejecuta como proceso del host.

## 3. Preparación que se implementará en fase 1

1. Instalar el SDK .NET 10 acordado, Aspire CLI, un motor de contenedores compatible y Node.js con el gestor elegido por frontend. Fijar versiones y registrar los comandos comprobados.
2. Crear el AppHost C# y registrar API, Vite y los contenedores. Usar SDK y paquetes estables compatibles con .NET 10, PostgreSQL 18 y las imágenes seleccionadas.
3. Modelar dependencias y comprobaciones de disponibilidad: PostgreSQL antes de Keycloak y API; Keycloak disponible antes de utilizar el login.
4. Fijar las imágenes de PostgreSQL y Keycloak según [backend.md §2](../03-architecture/backend.md). Configurar volúmenes de PostgreSQL compatibles con su versión.
5. Configurar Keycloak con almacenamiento PostgreSQL explícito, URL JDBC y credenciales de su base. Para mantener paquetes estables, se prevé modelarlo como contenedor de Aspire si la integración dedicada sigue en versión preliminar. La integración de identidad de la API mantiene JWT Bearer y añade introspección por petición protegida.
6. Preparar la importación local del realm y los usuarios de demostración con correspondencia de `sub`. Configurar `taskflow-spa` como cliente público y `taskflow-api` como cliente confidencial para introspección, con autenticación de cliente activada y secreto solo en la API. Verificar audiencia `taskflow-api`, roles, PKCE, rotación y sesión máxima de 10 horas.
7. Comprobar desde navegador y API las URL públicas, emisor, callback y CORS, además de conexión y persistencia tras un reinicio.

El AppHost será el punto de arranque del entorno. El `compose.yaml` de experimentación se conserva sin modificar y queda fuera de este flujo; iniciar ambos gestores sobre los mismos puertos produciría conflictos.

## 4. Bases de datos, configuración y secretos

Las bases lógicas `taskflow` y `keycloak` compartirán inicialmente un servidor PostgreSQL, con usuarios y contraseñas diferentes. Se preparará el alta de roles y permisos de cada base; declarar dos bases en AppHost no basta para separar sus credenciales.

AppHost inyectará la conexión de negocio en `ConnectionStrings__TaskFlow`; Keycloak recibirá únicamente la conexión y las credenciales de su propia base. La API no consultará las tablas internas del proveedor de identidad. Los parámetros secretos se guardarán en el mecanismo local de secretos y en configuración excluida de Git.

La aplicación utilizará permisos de operación ordinaria. La herramienta de migraciones y el procedimiento de mantenimiento usarán las credenciales técnicas que necesiten para cambiar el esquema o borrar usuarios, sin conceder esos permisos a la cuenta habitual de API. Sus conexiones se configurarán explícitamente al preparar el entorno.

| Configuración de API | Valor o procedencia |
|---|---|
| `ConnectionStrings__TaskFlow` | Conexión de negocio suministrada por AppHost. La herramienta de EF usará la misma base y credenciales apropiadas para migrar. |
| `Keycloak__Authority` | `http://localhost:8080/realms/taskflow`. |
| `Keycloak__Audience` | `taskflow-api`. |
| `Keycloak__Introspection__ClientId` | `taskflow-api`, cliente confidencial incluido en la audiencia del token. |
| `Keycloak__Introspection__ClientSecret` | Secreto del cliente de introspección, suministrado solo a la API desde el mecanismo local de secretos. No se incluye en Git, exportaciones del realm ni variables de Vite. |
| `Cors__AllowedOrigins__0` | `http://localhost:5173`. |
| `BusinessTimeZone` | `Europe/Madrid`. |
| `ASPNETCORE_ENVIRONMENT` | `Development`. |

Las variables de Vite son públicas: `VITE_API_BASE_URL`, `VITE_KEYCLOAK_URL`, `VITE_KEYCLOAK_REALM` y `VITE_KEYCLOAK_CLIENT_ID`. AppHost las suministrará al proceso de desarrollo con los valores públicos acordados, sin incluir secretos.

## 5. Arranque y migraciones por separado

El flujo que se comprobará al implementar la solución es:

1. Arrancar AppHost y esperar a que PostgreSQL y Keycloak estén disponibles.
2. En una base nueva o cuando se reciban migraciones pendientes, ejecutar explícitamente la herramienta de EF Core contra esa misma base. El comando y sus argumentos de proyecto y arranque se documentarán cuando existan las rutas reales.
3. Preparar los datos de demostración mediante una semilla repetible, coherente con el realm importado y sin duplicados.
4. Comprobar salud, login y recorridos de API/SPA antes de utilizar el entorno.

AppHost puede haber iniciado ya el proceso de API mientras se prepara el esquema. Ese proceso no migra la base; las operaciones de negocio se utilizarán después de aplicar las migraciones pendientes. No se incorporarán llamadas automáticas a `Database.Migrate`, `EnsureCreated` ni un recurso de migración que se ejecute en cada arranque.

Un reinicio ordinario conserva el esquema y los datos. La herramienta de EF consulta el historial y aplica solo las migraciones que faltan; no se genera una migración por crear o editar una tarea. El procedimiento de actualización se documentará también para una base vacía.

## 6. Verificación y reparto

- Backend prepara API, conexión, migraciones, roles de PostgreSQL, salud y datos locales.
- Frontend prepara Vite, configuración pública, cliente de API y flujo de sesión.
- Ambos revisan AppHost, dependencias, direcciones OIDC y arranque desde un clon limpio.
- Se probarán reinicio sin pérdida de datos, esquema desde cero, login, renovación y llamadas a `/me`.
- Una vez implementada la autenticación, reutilizar el mismo access token antes y después de completar logout o revocación administrativa de su sesión: respuesta admitida antes y 401 después. Si no puede consultarse introspección, devolver 503 sin ejecutar la operación y sin continuar solo con validación local; `/health` no requiere introspección.
- Las pruebas de backend con Testcontainers mantienen su PostgreSQL temporal aislado; no utilizarán los datos locales ni exigirán arrancar todo AppHost para cada prueba.

Eliminar volúmenes será una operación explícita documentada al implementar el entorno. El despliegue AWS permanece aplazado; usar Aspire en desarrollo no elige todavía una estrategia de publicación.

## 7. Referencias oficiales

- [Prerequisitos de Aspire y AppHost C#](https://aspire.dev/get-started/prerequisites/?apphost=csharp).
- [Integración de JavaScript y Vite](https://aspire.dev/integrations/frameworks/javascript/).
- [PostgreSQL: recursos y volúmenes](https://aspire.dev/integrations/databases/postgres/postgres-host/).
- [Keycloak: estado de la integración y configuración](https://aspire.dev/integrations/security/keycloak/).
