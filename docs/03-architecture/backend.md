# TaskFlow — Arquitectura del backend

> **Responsable:** David — backend
>
> **Estado:** definición técnica propia del backend completada para fase 0, con orquestación local mediante Aspire. Revocación en Keycloak con introspección por petición acordada; documentos originales protegidos hasta su aprobación. No hay código de backend ni migraciones implementadas.
>
> **Referencia:** copia de Documentos, especificación revisada el 2026-10-22. Roles `Admin` y `User`, visibilidad por pertenencia y gestión de tareas por los miembros del proyecto.
>
> **Actualización del análisis:** 2026-10-23. Acordados: versiones de §2, acceso a datos de §3, última escritura gana, validadores y consultas/paginación de §8, modelo físico de `database.md`, entorno/pruebas de §10–11, aplicación separada de migraciones en desarrollo, perfil local de §5 y operaciones simultáneas de §6. Para recursos concretos, un 404 por falta de visibilidad tiene prioridad sobre el 403 por operación administrativa. Frontend confirma los acuerdos de autenticación con sesión SSO máxima de 10 horas. Los ADR se completarán conforme avance el trabajo. Revocación cerrada por indicación del usuario: Keycloak e introspección en cada petición protegida.

## 1. Propósito y documentos relacionados

Este documento describe las responsabilidades, capas, dependencias, casos de uso, autorización y persistencia de la API. Toma como referencia la especificación nueva, confirmada durante la revisión, y no la matriz anterior de `Admin`/`Member` con lectura global.

- [Requisitos funcionales](../02-requirements/functional-requirements.md): comportamiento exigido y criterios de aceptación.
- [Visión general](system-overview.md): componentes y propuesta de separación en capas.
- [Modelo de datos](database.md): `AppUser`, `Project`, `AppUser_Project`, `Task` y restricciones.
- [Autenticación](authentication.md): OIDC, JWT y mapeo de roles.
- [Contrato de API](../05-api/api-contracts.md): rutas, DTO y respuestas del contrato inicial.
- [Frontend](frontend.md): integración, sesión, consultas y formularios.
- [Entorno local](../04-development/environment-setup.md): diseño del arranque mediante Aspire y pasos de implementación.
- [Coherencia documental](documentation-alignment.md): alcance de esta actualización y cambios pendientes de aprobación en documentos del manager.
- [ADR](adr/README.md): decisiones propuestas y su aprobación.

Se distingue entre requisitos, acuerdos de backend, acuerdos de integración confirmados con frontend y la decisión de revocación confirmada por el usuario. Los pendientes de §12 permanecen identificados; completar la definición del backend no equivale a dar por aprobada toda la arquitectura ni a tenerla implementada.

## 2. Tecnologías y alcance

La base prevista es C#, ASP.NET Core, Entity Framework Core, PostgreSQL y Keycloak. Por indicación del usuario, Aspire será el orquestador del entorno de desarrollo. Se acuerda utilizar las versiones estables más recientes que formen una combinación compatible, excluyendo versiones preliminares o RC.

### Versiones de referencia verificadas el 2026-10-23

| Componente | Versión seleccionada como base para fase 1 | Compatibilidad |
|---|---|---|
| SDK de .NET | `10.0.402` | SDK estable para desarrollar proyectos `net10.0`, con C# 14. |
| Runtime .NET y ASP.NET Core | `10.0.13`, rama .NET 10 LTS | Último parche estable verificado de la rama 10. |
| Paquetes `Microsoft.EntityFrameworkCore.*` y herramienta `dotnet-ef` | `10.0.13` | Mantener alineados los paquetes de EF Core de Microsoft. |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | `10.0.3` | Requiere .NET 10 y EF Core desde `10.0.4` hasta antes de `11.0.0`; admite EF Core `10.0.13`. |
| PostgreSQL | `18.6` | Última rama estable verificada; admite el `SET NULL` parcial que necesita el modelo. |
| Keycloak Server | `26.8.1` | Versión estable verificada. Su matriz de bases de datos admite PostgreSQL 18. |

Fuentes de versiones: [SDK y runtime .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), [EF Core](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/), [dependencias de Npgsql](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3), [PostgreSQL](https://www.postgresql.org/support/versioning/), [Keycloak](https://www.keycloak.org/downloads) y su [matriz de bases de datos](https://www.keycloak.org/server/db).

El AppHost de Aspire se desarrollará en C# sobre el SDK .NET 10 acordado. La versión estable exacta del SDK de AppHost, CLI e integraciones se fijará al crear el entorno, comprobando compatibilidad con PostgreSQL 18 y las imágenes seleccionadas. Esto no añade una quinta capa de negocio.

La compatibilidad anterior es la declarada por los componentes; todavía no se ha comprobado mediante restauración de paquetes, migraciones o integración del proyecto. En fase 1 se fijarán las versiones exactas del SDK, paquetes e imágenes en los archivos de configuración, sin etiquetas flotantes `latest`, y se verificará el arranque completo. Los parches nuevos se adoptarán mediante una actualización revisada. El `compose.yaml` de prueba existente no se considera actualizado por este documento.

El backend es una única API desplegable. No se requieren microservicios, colas, CQRS, mediadores ni repositorios genéricos para cumplir el MVP. Se acuerda organizarlo en cuatro proyectos: `TaskFlow.Api`, `TaskFlow.Application`, `TaskFlow.Domain` y `TaskFlow.Infrastructure`, siguiendo la separación de Clean Architecture. No implica adoptar DDD completo. El ADR correspondiente se redactará conforme avance el trabajo; el acuerdo queda registrado aquí.

## 3. Capas, responsabilidades y dependencias

| Proyecto acordado | Responsabilidad | Dependencias |
|---|---|---|
| `TaskFlow.Domain` | Entidades, valores admitidos y reglas de negocio que no dependen del transporte ni de la persistencia. | Ninguna de las otras capas; sin ASP.NET Core, EF Core o Keycloak. |
| `TaskFlow.Application` | Casos de uso, comprobaciones por recurso, coordinación de transacciones y contratos de acceso a datos e identidad actual. | `Domain`. |
| `TaskFlow.Infrastructure` | EF Core, configuración de entidades, PostgreSQL, migraciones e implementación de contratos de persistencia. | `Application` y `Domain`. |
| `TaskFlow.Api` | Endpoints, DTO HTTP, validación de entrada, autenticación JWT, mapeo de roles, políticas y traducción de resultados a HTTP. | `Application`; `Infrastructure` para registrar implementaciones en el arranque. |

Los endpoints llaman a los casos de uso; no consultan directamente la base de datos. `Application` recibe la identidad actual mediante un contrato, sin depender de `HttpContext` ni de la estructura del token de Keycloak. Las entidades de `Domain` no contienen DTO HTTP ni conocen los roles anidados del proveedor de identidad.

Se acuerda acceder a datos desde `Application` mediante interfaces de repositorios específicos, implementadas en `Infrastructure`. Los métodos concretos se definirán según los casos de uso; no se introduce una abstracción genérica para cada tabla.

### Acceso a datos acordado

Los casos de uso se agrupan en servicios por funcionalidad, por ejemplo `ProjectService` y `TaskService`, en `Application`. Cada método representa un caso de uso: crear proyecto, archivar proyecto, asignar tarea o cambiar su estado. No se exige una clase independiente por cada acción.

| Elemento | Ubicación | Función |
|---|---|---|
| `ProjectService`, `TaskService` | `Application` | Coordinan reglas, permisos y pasos del caso de uso. |
| `IProjectRepository`, `ITaskRepository` y contratos necesarios de consulta | `Application` | Definen las operaciones de datos que necesita la aplicación, sin exponer `DbContext`, `DbSet` o `IQueryable` de EF Core a los servicios. |
| `ProjectRepository`, `TaskRepository` | `Infrastructure` | Implementan los contratos mediante EF Core. |
| `TaskFlowDbContext` | `Infrastructure` | Configura el modelo de EF Core, consulta PostgreSQL y realiza el seguimiento y guardado de cambios. |
| Contrato de guardado/transacciones, por ejemplo `IUnitOfWork` | `Application`, implementado en `Infrastructure` | Permite guardar conjuntamente los cambios de un caso de uso. Reutiliza el contexto; no añade otra base de datos ni otro contexto independiente. |

El flujo es `Controller → servicio de Application → contrato de repositorio → implementación con TaskFlowDbContext → PostgreSQL`. La inyección de dependencias se configura en el arranque de `Api`.

No se crea un repositorio por cada tabla automáticamente. Las pertenencias pueden gestionarse mediante operaciones del repositorio de proyectos y el panel mediante un contrato de consulta específico. Los repositorios comparten el contexto dentro de la operación y no guardan cada cambio de forma independiente. Una creación de proyecto y pertenencia debe confirmarse completa o deshacerse completa.

### Organización del backend

La separación en cuatro proyectos está acordada. La distribución interna en carpetas y los nombres de los proyectos de pruebas siguen siendo orientativos.

```text
backend/
├── src/
│   ├── TaskFlow.Api/
│   ├── TaskFlow.Application/
│   │   ├── Projects/
│   │   ├── Tasks/
│   │   ├── Users/
│   │   └── Dashboard/
│   ├── TaskFlow.Domain/
│   └── TaskFlow.Infrastructure/
└── tests/
    ├── TaskFlow.Domain.Tests/
    └── TaskFlow.Api.IntegrationTests/
```

El proyecto de orquestación se prevé en `infrastructure/TaskFlow.AppHost/`, separado de las cuatro capas de negocio. Allí se definirá el arranque de la API, Vite, PostgreSQL y Keycloak; sus instrucciones estarán en `docs/04-development/`. No se crean esos proyectos en esta fase documental.

La estructura todavía no está implementada. Se acuerda utilizar **Controllers de ASP.NET Core** para exponer la API. Los Controllers reciben y validan los DTO HTTP, invocan los casos de uso de `Application` y traducen sus resultados a respuestas HTTP; las reglas de negocio y el acceso directo a datos no se sitúan en ellos.

## 4. Casos de uso y permisos

| Área | Casos de uso | Acceso según la especificación nueva |
|---|---|---|
| Identidad | Resolver o crear la referencia local; consultar perfil; listar usuarios asignables. | Token válido con `Admin` o `User`. |
| Proyectos | Listar, consultar y crear. | `Admin` ve todos; `User` solo los activos de los que es miembro. Ambos pueden crear. |
| Proyectos | Editar nombre y descripción. | `Admin` sobre cualquiera; `User` sobre sus proyectos activos. |
| Administración de proyectos | Archivar, listar archivados, restaurar y eliminar físicamente. | Solo `Admin`. |
| Tareas | Crear, consultar, editar, eliminar físicamente, asignar y cambiar estado. | `Admin` en cualquier proyecto, incluidos archivados; `User` en los activos de los que es miembro. |
| Mis tareas | Consultar tareas asignadas al usuario actual. | Ambos roles; solo proyectos activos. |
| Panel | Consultar recuentos de proyectos activos y tareas abiertas, completadas y vencidas. | Global para `Admin`; proyectos activos de pertenencia para `User`. |

En este modelo, `User` puede gestionar todas las tareas de sus proyectos visibles, incluidas las asignadas a otras personas o sin asignar. No se mantiene la antigua restricción de cambiar solo el estado de las tareas propias.

El registro e inicio de sesión se realizan en Keycloak. No se crean endpoints propios para recibir contraseñas, emitir tokens o promover usuarios a `Admin`. El administrador de arranque de Keycloak tampoco equivale automáticamente al rol `Admin` de TaskFlow.

## 5. Identidad, autorización y visibilidad

### Autenticación

La API recibe el access token en `Authorization: Bearer ...`, valida firma, emisor, audiencia y vigencia con JWT Bearer y consulta su estado mediante introspección en Keycloak en cada petición protegida, antes de ejecutar el caso de uso. Un ID token no se admite como credencial de acceso a la API. La API responde con códigos HTTP; la SPA gestiona las redirecciones a Keycloak.

Se acuerda extraer únicamente `Admin` y `User` de `realm_access.roles` mediante una transformación de reclamaciones, conforme al diseño de [authentication.md](authentication.md). La transformación debe evitar duplicar reclamaciones si se ejecuta varias veces y configurar explícitamente el tipo de reclamación utilizado como rol. Los nombres de realm, clientes y audiencia y los tiempos de sesión se confirman con frontend; la política de revocación queda acordada en este análisis.

### Configuración de autenticación acordada con frontend

| Elemento | Acuerdo |
|---|---|
| Realm | `taskflow`. |
| Cliente público de la SPA | `taskflow-spa`, Authorization Code con PKCE `S256`, sin secreto en el navegador. |
| Audiencia del access token | `taskflow-api`, incluida mediante el mapper de Keycloak y validada por la API. |
| Roles | Roles de realm `Admin` y `User`; `User` por defecto al registrarse y `Admin` asignado por administración de Keycloak. |
| Inicio y registro | Gestionados por Keycloak. La SPA redirige y la API no recibe contraseñas. |
| Access token | Duración de 5 minutos. |
| Sesión SSO | 30 minutos de inactividad de sesión y 10 horas de duración máxima, según la respuesta de frontend; la renovación queda sujeta a esos límites. |
| Desfase de reloj admitido por la API | 30 segundos, configurados explícitamente. |
| Cierre de sesión | Finalizar sesión en Keycloak y limpiar tokens y caché en la SPA. Validación JWT más introspección obligatoria por petición protegida: token inactivo → 401; fallo de comprobación → 503. |
| Perfil local | Acordado en el apartado de perfil: resolver por `sub`, crear la referencia local en el primer acceso y sincronizar desde `/me`. La SPA consultará `/me` después del login y tras renovar el token. |

Se acuerda activar la rotación de refresh tokens y coordinar su renovación en la SPA para no lanzar renovaciones simultáneas innecesarias. La configuración y el funcionamiento con varias pestañas se verificarán al implementar Keycloak y la SPA. Estos acuerdos se reflejan en [authentication.md](authentication.md); su implementación y comprobación siguen pendientes.

Las 10 horas máximas equivalen a 36.000 segundos y coinciden con `DEFAULT_SESSION_MAX_LIFESPAN` de [Keycloak 26.8.1](https://github.com/keycloak/keycloak/blob/26.8.1/server-spi-private/src/main/java/org/keycloak/models/Constants.java#L62-L63), la versión de referencia seleccionada. Se conserva ese valor predeterminado para este parámetro.

### Respuestas confirmadas por frontend

El responsable de frontend acepta todos los puntos consultados y cambia la duración máxima de la sesión SSO a 10 horas. La revocación se cierra por indicación posterior del usuario, según el apartado siguiente.

1. Usar el realm `taskflow`, el cliente público `taskflow-spa` y la audiencia `taskflow-api`, con roles de realm `Admin` y `User`.
2. Usar `keycloak-js` para login y registro con Authorization Code + PKCE `S256`, guardar los tokens en memoria y enviar a la API solo el access token.
3. En local: SPA en `http://localhost:5173`, callback `http://localhost:5173/callback`, retorno del logout a `http://localhost:5173/`, API en `http://localhost:5080` y Keycloak en `http://localhost:8080`, con orígenes CORS exactos.
4. Access tokens de 5 minutos, sesión SSO con 30 minutos de inactividad de sesión y 10 horas máximas, y 30 segundos de tolerancia de reloj en la API. La renovación cuenta como actividad de sesión; esto no equivale a cerrar sesión tras 30 minutos sin mover el ratón.
5. Renovar antes de enviar peticiones cuando queden menos de 30 segundos de token, coordinar una sola renovación simultánea por instancia de SPA y activar rotación de refresh tokens. Verificar también el funcionamiento con varias pestañas.
6. Al completar login y tras renovar el token, consultar `GET /api/v1/me` para recuperar el perfil local y los roles vigentes.
7. Si la sesión ya no puede renovarse, limpiar tokens y caché protegida y volver al login; para 403, mostrar acceso denegado; para 404, el mismo mensaje de recurso no encontrado. Las escrituras ya enviadas no se reintentan automáticamente.

El almacenamiento en memoria y la renovación previa utilizan las capacidades del [adaptador JavaScript de Keycloak](https://www.keycloak.org/securing-apps/javascript-adapter). Los tiempos y la rotación se configurarán según estos acuerdos usando la [configuración de sesiones de Keycloak](https://www.keycloak.org/docs/latest/server_admin/#_timeouts).

### Revocación en Keycloak e introspección por petición

**Decisión cerrada el 2026-10-23 por indicación del usuario.** Keycloak gestiona la revocación de la sesión/token. La API mantiene la validación JWT local y, tras superarla, consulta introspección en cada petición protegida antes de resolver el usuario local o ejecutar el caso de uso.

- Usar el cliente confidencial `taskflow-api`, con autenticación de cliente activada y secreto solo en la API. Este identificador ya está incluido en `aud`; Keycloak 26.8.1 exige que el cliente de introspección figure en la audiencia. No requiere permisos de administración del realm.
- Consultar `POST /realms/taskflow/protocol/openid-connect/token/introspect` con el access token y credenciales del cliente. Configurar un tiempo de espera acotado.
- `active: false` → 401. Indisponibilidad, tiempo de espera agotado, respuesta inválida o fallo de credenciales del cliente → 503, sin ejecutar operaciones de negocio ni continuar con la validación local como alternativa.
- No almacenar resultados positivos en caché entre peticiones. Una única introspección por petición basta aunque se evalúe varias veces la autenticación; sí se conservan los metadatos OIDC y JWKS en caché.
- Tras completar el cierre de sesión en Keycloak, el mismo access token debe ser rechazado con 401 en la siguiente petición. Comprobar también la revocación administrativa de la sesión. Borrar tokens en la SPA o revocar solo un refresh token no equivale por sí mismo a esa garantía.
- Las peticiones autorizadas antes del cierre no se cancelan retroactivamente. `/health` es anónimo y no requiere introspección.

Se mantienen los tiempos de sesión acordados y no se añade una lista propia de revocación. La tolerancia de reloj local no concede un plazo adicional cuando Keycloak declara el token inactivo. El coste aceptado es una llamada a Keycloak por petición protegida y depender de su disponibilidad.

El flujo, la configuración y las pruebas se detallan en [authentication.md §5](authentication.md). Fuentes: [endpoints de Keycloak](https://www.keycloak.org/securing-apps/oidc-layers) e [introspección 26.8.1](https://github.com/keycloak/keycloak/blob/26.8.1/services/src/main/java/org/keycloak/protocol/oidc/AccessTokenIntrospectionProvider.java).

### Usuario local

La identidad se obtiene del `sub` validado y se resuelve mediante `AppUser.keycloak_sub`. Para nuevos usuarios registrados con un rol admitido, se creará la referencia local en su primera solicitud. La unicidad de `keycloak_sub` debe permitir recuperar la misma fila ante solicitudes simultáneas, sin crear duplicados ni fallar por una carrera esperable.

Los usuarios de demostración pueden precargarse para poder asignarles tareas antes de su primer acceso. Los usuarios registrados que aún no tienen referencia local no aparecen en la lista de asignables, conforme a la propuesta actual de autenticación.

### Perfil local acordado

Keycloak será la fuente del perfil; `AppUser` conservará una copia mínima para relacionar los datos de negocio. La API utilizará únicamente claims del access token ya validado, sin consultar la API de administración de Keycloak en cada petición.

| Aspecto | Decisión acordada |
|---|---|
| Identidad | Relacionar por `sub`, sin modificarlo ni usar correo o nombre como identificador. Si falta o no cabe en `keycloak_sub`, rechazar la credencial con 401 sin crear una fila parcial. |
| Alta | Crear `AppUser` en la primera petición de negocio autorizada de una identidad que todavía no tenga referencia local; recuperar la misma fila ante altas simultáneas mediante la unicidad de `keycloak_sub`. |
| Actualización | Sincronizar nombre y correo cuando se consulte `GET /api/v1/me`, después del login y tras renovar el token; escribir solo si cambian. Las demás peticiones resuelven el usuario, sin reescribir su perfil. |
| Nombre | Usar `name`; si está vacío, `preferred_username`; si tampoco existe, `Usuario` seguido de una parte del UUID local. Recortar espacios y limitar la copia visible a 200 caracteres, sin modificar el perfil de Keycloak. No usar el correo como nombre de reserva. |
| Correo | Opcional; recortar espacios. Si falta, está vacío, tiene formato inválido o supera 320 caracteres, guardar `NULL` y registrar la incidencia de perfil sin impedir las operaciones de negocio. No truncar una dirección de correo. |
| Roles | Obtenerlos del token de cada petición; no persistirlos ni deducirlos del nombre o correo. |
| Datos expuestos | En listas y selectores, solo identificador local y nombre visible. El perfil propio puede incluir el correo según el contrato. |
| Cambios en Keycloak | Se reflejan cuando se emite un token con los nuevos datos y la SPA vuelve a consultar `/me`; no se promete sincronización instantánea. |
| Identidad eliminada o deshabilitada | Conservar la referencia local, pertenencias, autorías y tareas. No convertir una eliminación en Keycloak en un borrado automático de datos de negocio. La introspección rechaza los tokens que Keycloak considere inactivos. Esto no sincroniza el estado de otras referencias locales. |

**Limitación aceptada para el MVP:** sin una sincronización adicional, la API no mantiene actualizado el estado de actividad de todas las identidades locales. Comprobar el token de quien hace una petición no actualiza la lista de otras identidades deshabilitadas o eliminadas en Keycloak. Su referencia local puede seguir apareciendo en asignables. Excluirla automáticamente exigiría una marca local de actividad y un procedimiento para mantenerla o una integración adicional; no se incorpora como comportamiento implícito. Se acepta expresamente esta limitación junto con el funcionamiento del perfil local.

La eliminación local de `AppUser`, cuando se realice mediante el procedimiento de mantenimiento previsto, sigue las reglas ya acordadas de último miembro y desasignación. Es una operación distinta de eliminar o deshabilitar la identidad en Keycloak.

### Acceso a recursos

- Todas las operaciones de negocio exigen un rol admitido. `/health` es la excepción pública propuesta.
- Los identificadores enviados por el cliente permiten seleccionar recursos, pero no acreditan la identidad ni los permisos del solicitante.
- Para `User`, las consultas de proyectos y tareas se filtran por pertenencia y proyecto activo antes de devolver datos o permitir cambios.
- La pertenencia se comprueba en la consulta o caso de uso, incluidas listas, filtros, detalle, edición y recuentos. No se resuelve ocultando botones ni filtrando después de cargar todos los datos.
- Para `Admin`, no se aplica el filtro de pertenencia. Las listas activas, «Mis tareas» y el panel siguen excluyendo archivados.
- El dato `created_by` identifica al autor; la visibilidad depende de `AppUser_Project`. No se añade un rol independiente de propietario.

### Códigos y precedencia de autorización

| Situación | Respuesta prevista |
|---|---|
| Token ausente, inválido, caducado o inactivo según introspección | 401. |
| No se puede comprobar la introspección en Keycloak | 503, sin ejecutar el caso de uso ni continuar solo con la validación local. |
| Token válido sin `Admin` ni `User` | 403. |
| Recurso concreto inexistente o no visible para `User`, también en operaciones administrativas | 404, conforme a BR-015. Tiene prioridad sobre el 403 de la operación administrativa una vez autenticado el usuario y comprobado un rol admitido. |
| `User` intenta una operación administrativa sobre un proyecto visible | 403. |
| `User` solicita una operación administrativa sin recurso concreto, como listar archivados | 403. No hay un recurso individual cuya existencia ocultar. |
| Lista o panel sin proyectos visibles | 200 con lista vacía o recuentos cero. No 404 por el mero hecho de no tener pertenencias. |

Se acuerda esta precedencia para las operaciones sobre un identificador concreto: autenticar y exigir un rol admitido; buscar el recurso con la visibilidad del solicitante; devolver 404 si no existe para él; y solo si es visible comprobar si la operación exige `Admin`, devolviendo 403 cuando proceda. La respuesta 404 usará el mismo formato y mensaje para recurso inexistente y no visible. No cambia los 401 por credencial inválida ni los 403 por ausencia de rol admitido.

Por tanto, en endpoints administrativos con identificador no se aplicará una política `AdminOnly` que interrumpa la petición antes de comprobar la visibilidad: se mantendrá la política general de usuario autenticado con rol admitido y el caso de uso coordinará visibilidad y autorización de la operación. En operaciones administrativas sin recurso concreto sí puede comprobarse directamente `AdminOnly`. La ocultación mediante 404 está contemplada en [RFC 9110, §15.5.4](https://www.rfc-editor.org/rfc/rfc9110.html#name-403-forbidden).

## 6. Reglas de negocio y operaciones atómicas

| Operación | Reglas y garantía prevista |
|---|---|
| Crear proyecto | Nombre válido; el backend fija el autor autenticado e inserta proyecto y pertenencia del creador en una misma transacción. |
| Crear tarea | Proyecto existente y accesible para la operación; título válido, prioridad y estado admitidos; vencimiento y asignado opcionales. Si hay asignado, aplicar también las reglas de asignación. |
| Asignar o reasignar | Usuario local válido. Si no es miembro, crear su pertenencia antes de guardar la asignación, en la misma transacción y sin duplicar la pareja usuario-proyecto. |
| Desasignar o eliminar tarea | Conservar la pertenencia del usuario anterior en el MVP. La eliminación de la tarea es física según ADR-0002, todavía propuesto. |
| Cambiar estado | Validar el estado solicitado y los permisos sobre el proyecto. El contrato propone permitir cualquier transición entre los tres estados. |
| Editar tarea | No cambiar su proyecto. Edición general, estado y asignación tienen DTO y operaciones separados. |
| Archivar o restaurar | Solo `Admin`. Cambiar `archived_at` sin borrar tareas ni pertenencias. |
| Eliminar proyecto | Solo `Admin`. Borrado físico con cascada a tareas y pertenencias según ADR-0002. Los usuarios se conservan. |

Las altas de pertenencia concurrentes se resolverán mediante la clave única compuesta y una inserción que tolere una pertenencia ya existente, como propone `database.md`. Esto garantiza la unicidad de la relación; no añade idempotencia a la creación de proyectos o tareas.

La autorización, comprobación de estado del proyecto y escritura deben ser coherentes dentro de la operación. Se acuerda resolver las carreras entre archivado y modificación mediante las transacciones y bloqueos definidos en esta sección.

### Concurrencia de ediciones: decisión para el MVP

Se acuerda **última escritura gana**: una escritura válida posterior puede sobrescribir los mismos campos guardados por otra persona. No se exigen tokens de versión ni cabeceras `ETag`/`If-Match` para modificar recursos en el MVP. La detección de conflictos queda recogida como mejora posterior en el [backlog](../backlog.md), y requerirá actualizar el contrato y la interfaz.

Esta decisión no elimina las transacciones ni las restricciones de integridad. Evitar pertenencias duplicadas, completar operaciones relacionadas de forma atómica y proteger al último miembro sigue siendo obligatorio.

No se incorporan endpoints de retirada manual de miembros ni de eliminación de usuarios al MVP. Si se elimina un usuario local por el procedimiento controlado de mantenimiento, se bloqueará la operación completa cuando sea el último miembro de algún proyecto, activo o archivado, y se informará de que debe incorporarse otro miembro antes de eliminarlo. Si se permite el borrado, sus tareas se conservan sin asignar. La comprobación y los cambios serán atómicos mediante los bloqueos ordenados acordados a continuación; su implementación y pruebas corresponden a la siguiente fase.

Esta regla no bloquea eliminar el proyecto: `Admin` puede borrar un proyecto activo o archivado en cualquier momento, aunque tenga un único miembro, con las cascadas ya previstas.

### Operaciones simultáneas: mecanismo acordado

Se acuerda utilizar transacciones cortas con aislamiento `Read Committed` y un bloqueo de fila de PostgreSQL sobre el proyecto (`SELECT ... FOR UPDATE`) al modificar sus datos, tareas o pertenencias. El bloqueo se adquiere antes de comprobar el estado actual y realizar cambios, y se libera al confirmar o deshacer la transacción. No se mantiene mientras la persona rellena un formulario.

Todas las escrituras que afecten al mismo proyecto deben seguir este protocolo. La comprobación de visibilidad, estado activo y permisos se repite con los datos actuales tras adquirir el bloqueo; las tareas se vuelven a consultar dentro de esa operación. No basta con una comprobación previa fuera de la transacción. Los proyectos diferentes pueden modificarse en paralelo y las lecturas normales no necesitan esos bloqueos de escritura.

| Operaciones que coinciden | Resultado acordado |
|---|---|
| Archivar y editar como `User` | Si se confirma primero el archivado, la edición comprueba el estado nuevo y responde 404. Si se confirma primero la edición, sus cambios se conservan y después se archiva. `Admin` mantiene sus permisos sobre archivados. |
| Eliminar tarea y asignarla | Si la eliminación se confirma primero, la asignación devuelve 404 sin crear una pertenencia. Si se confirma primero la asignación, después se elimina la tarea y se conserva la pertenencia, según la regla del MVP. |
| Eliminar proyecto y modificar una tarea suya | Si se confirma primero el borrado del proyecto, la modificación devuelve 404. Si la modificación se confirma antes, el borrado posterior elimina el proyecto y sus tareas en cascada. |
| Dos asignaciones al mismo usuario y proyecto | No se duplican pertenencias; se conservan la clave compuesta y la inserción que admite un registro ya existente. |
| Dos borrados locales de usuarios del mismo proyecto | Las comprobaciones se realizan de forma ordenada. Si el primer borrado deja un único miembro, el segundo detecta que es el último y se rechaza íntegramente. |

**Procedimiento de eliminación local de usuario acordado:**

1. Iniciar una transacción y bloquear la fila del usuario que se elimina, antes de consultar sus pertenencias. Las altas de pertenencia o autoría protegen antes la identidad referenciada con un bloqueo compatible entre lectores (`FOR KEY SHARE`), para que una eliminación no pueda adelantarse a una nueva relación ya en curso.
2. Bloquear los proyectos existentes a los que pertenece, incluidos archivados, siempre en el mismo orden por UUID. Después de adquirir los bloqueos, consultar de nuevo los miembros de esos proyectos.
3. Si en alguno es el último miembro, deshacer toda la operación y comunicar que antes debe incorporarse otro miembro. No modificar tareas, pertenencias ni autorías.
4. En otro caso, eliminar el usuario dentro de la misma transacción y aplicar las cascadas y `SET NULL` acordados.

Se utilizará un orden común cuando una operación necesite varios bloqueos: primero las identidades referenciadas y después los proyectos, con orden estable dentro de cada grupo. `Infrastructure` implementará los detalles SQL a través de los contratos de persistencia; `Application` coordinará la transacción compartida, sin guardar cambios parciales desde cada repositorio.

No se añade un endpoint de borrado de usuarios. Se admite ese borrado únicamente mediante un procedimiento de mantenimiento controlado que ejecute estos pasos; la cuenta habitual de la API no tendrá permiso de borrar directamente usuarios ni retirar pertenencias manualmente. Las cascadas al eliminar un proyecto siguen permitidas. Un `DELETE` administrativo que omita el procedimiento no queda protegido por la comprobación del último miembro; si se habilitan otros escritores, necesitarán la misma garantía o una protección adicional en la base.

Este mecanismo conserva última escritura gana para las ediciones del MVP: ordena la ejecución de las escrituras, pero no detecta que una persona editó una versión antigua ni añade `ETag`, versión o bloqueo de pantalla. Se probarán especialmente las carreras de la tabla y el borrado de un proyecto con un único miembro. El mecanismo acordado se apoya en los [bloqueos de fila de PostgreSQL](https://www.postgresql.org/docs/18/explicit-locking.html) y su [aislamiento Read Committed](https://www.postgresql.org/docs/18/transaction-iso.html).

## 7. Persistencia y migraciones

`Infrastructure` implementará el acceso con EF Core y el proveedor de PostgreSQL elegido. El mapeo seguirá [database.md](database.md): claves primarias y únicas, pertenencia N:M, autor opcional tras su eliminación, tareas con proyecto obligatorio y asignación opcional.

La FK compuesta de asignación debe conservar `project_id` al eliminar la pertenencia y dejar solo `assigned_id` a `NULL`. PostgreSQL 15 documenta `ON DELETE SET NULL (assigned_id)`; falta comprobar el SQL generado por la combinación elegida de EF Core y proveedor. Un `DeleteBehavior.SetNull` genérico sobre toda la clave compuesta no debe darse por equivalente.

Se configurará el mismo orden de propiedades en la clave principal y en la FK compuesta. El modelo físico acordado usa PK de pertenencia (`project_id`, `user_id`) y FK de tarea (`project_id`, `assigned_id`), de modo que ambas quedan alineadas sin una clave alternativa adicional.

Las migraciones deberán probarse desde una base vacía y con datos de ejemplo. Se comprobarán especialmente la tarea sin asignar, la asignación a miembro, la eliminación local de usuario y la cascada de un proyecto con tareas asignadas. La alternativa de desasignar desde un caso de uso no protege un borrado directo en SQL; cualquier alternativa debe documentar qué garantiza la base y qué garantiza la aplicación.

El modelo físico acordado se recoge en [database.md](database.md): identificadores UUID generados por el backend, longitudes de texto, estados y prioridades como texto con `CHECK`, vencimiento `due_date` de tipo `date` e instantes `timestamptz`. Aquí se describen las responsabilidades de persistencia, mapeo y migración. Las migraciones de negocio no modifican las tablas internas de Keycloak.

## 8. Contrato HTTP, consultas y fechas

Se sigue el contrato inicial de [api-contracts.md](../05-api/api-contracts.md): prefijo `/api/v1`, JSON con campos `camelCase`, DTO específicos, instantes UTC y vencimiento como fecha de calendario. Las entidades de EF Core no se exponen directamente en las respuestas.

- Filtrar por visibilidad antes de contar, ordenar y paginar. El cliente no puede ampliar su acceso cambiando `projectId` o `assigneeId`.
- La paginación de proyectos y tareas, los tamaños predeterminados y máximos y la ordenación se acuerdan según la tabla de esta sección.
- Los campos de ordenación se limitan a los admitidos y quedan reflejados en el contrato.
- «Mis tareas» deriva el usuario del token y conserva el filtro de proyectos activos.
- Los recuentos del panel se calculan sobre todos los registros relevantes, no solo la página solicitada.
- La API calcula `overdueTasks`, `businessDate` y `businessTimeZone` para el panel usando `Europe/Madrid`. El frontend calcula `isOverdue` para cada tarea; el atributo no se persiste ni se envía en el DTO de tarea.
- Las marcas temporales se generan en backend en UTC. Una fecha de vencimiento no se convierte automáticamente en un instante UTC.

Se propone un reloj inyectable para poder probar cambios de día y límites de fecha sin depender de la hora real del equipo. La biblioteca o mecanismo concreto se elegirá con la versión de .NET.

### Consultas, paginación y OpenAPI acordados

El tamaño de página es la cantidad máxima de elementos de una lista que devuelve una petición. Por ejemplo, `page=2&pageSize=20` solicita la segunda página de veinte elementos del resultado filtrado y ordenado.

| Aspecto | Decisión |
|---|---|
| Parámetros | `page` desde 1 y `pageSize` entre 1 y 100. |
| Predeterminados | `page=1`, `pageSize=20`; un parámetro con valor inválido responde 400. |
| Respuesta | `items`, `page`, `pageSize` y `totalCount`; el total corresponde a todos los registros del filtro autorizado, antes de paginar. |
| Página posterior a la última | 200 con `items` vacío y el total real. |
| Orden de proyectos | `createdAt desc`, después `id asc`. |
| Orden predeterminado de tareas | `sort=createdAt&order=desc`; después `id asc`. |
| Orden por vencimiento | `sort=dueDate&order=asc` por defecto; se admite `desc`. |
| Sin vencimiento | Siempre al final en ambas direcciones. |
| Desempate por vencimiento | `createdAt desc` y después `id asc`. |
| OpenAPI | Generarlo desde los Controllers al implementar la API y versionar el resultado en `docs/05-api/`. Hasta entonces, el contrato Markdown guía las simulaciones y la coordinación con frontend. |

La ordenación se aplica en la base de datos antes de paginar. El desempate define un orden determinista para un mismo conjunto de datos; no garantiza una fotografía inmutable entre peticiones si otros usuarios insertan o borran registros.

### Validación y errores

El backend valida entradas y reglas aunque el frontend ya las compruebe. Nombres y títulos no pueden estar vacíos tras recortar espacios; estados, prioridades, fechas, filtros y parámetros de paginación deben ser válidos.

Se acuerda distribuir las comprobaciones de validación de la siguiente forma:

| Nivel | Comprobaciones | Mecanismo acordado |
|---|---|---|
| Entrada HTTP | Cuerpo requerido, campos obligatorios, tipos, longitudes, valores de estado/prioridad, fechas y rangos de paginación. | DTO con Data Annotations y validación de Controllers con `[ApiController]`; comprobaciones explícitas para reglas de entrada que no cubran los atributos. |
| Caso de uso | Existencia de usuarios y recursos, visibilidad, operación permitida, proyecto activo cuando corresponda y protección del último miembro. | Comprobaciones en servicios de `Application`, consultando datos cuando haga falta. |
| Dominio | Invariantes que deben mantenerse independientemente de HTTP. | Métodos y construcción de entidades en `Domain`. |
| Base de datos | Unicidad de `keycloak_sub`, unicidad de la pareja usuario-proyecto y validez de claves foráneas. | Restricciones configuradas en las migraciones. |

No toda entrada debe ser no nula: descripción, vencimiento y asignado son opcionales. Tampoco toda repetición es un error: los requisitos no exigen nombres de proyecto ni títulos de tarea únicos. Las comprobaciones previas de unicidad no sustituyen las restricciones, que también protegen ante solicitudes simultáneas.

### Validadores concretos acordados

Se aceptan los siguientes límites junto con el modelo físico. Se recortarán los espacios exteriores de nombres, títulos y descripciones; los textos opcionales vacíos se normalizarán a `null`.

| Entrada | Comprobaciones acordadas |
|---|---|
| Cuerpo de una escritura | JSON válido, objeto no nulo y propiedades admitidas por su DTO. Rechazar propiedades desconocidas con 400, conforme al contrato. |
| Proyecto: `name` | Obligatorio; entre 1 y 150 caracteres después de recortar; rechazar solo espacios. Se permiten nombres repetidos. |
| Proyecto o tarea: `description` | Opcional; `null` o hasta 5.000 caracteres; vacío tras recortar equivale a `null`. |
| Tarea: `title` | Obligatorio; entre 1 y 200 caracteres después de recortar; se permiten títulos repetidos. |
| Crear tarea: `status` | Si se omite, usar `ToDo`; si se envía, solo `ToDo`, `InProgress` o `Done`. Rechazar `null`, números y valores desconocidos. El dato persistido siempre es obligatorio, aunque el cliente pueda omitirlo en la creación. |
| Tarea: `priority` | Obligatoria en creación y edición general; solo `Low`, `Medium` o `High`, sin números ni `null`. |
| Tarea: `dueDate` | Opcional; `null` o fecha válida `YYYY-MM-DD`, sin hora. Admitir fechas anteriores a hoy. |
| Crear tarea: `assigneeId` | Opcional; `null` o identificador de usuario local existente. Si no es miembro, crear la pertenencia en la misma operación. Usuario desconocido → 400 en el campo. |
| Operación de asignación | La propiedad `assigneeId` debe estar presente; `null` desasigna. Un cuerpo vacío no desasigna accidentalmente. |
| Operación de cambio de estado | `status` obligatorio y perteneciente al conjunto admitido. Las demás propiedades no se modifican. |
| Identificadores | UUID válido para los identificadores locales; formato inválido → 400. Recurso bien identificado pero inexistente o no visible → 404 según las reglas de acceso. |
| Filtros | Estado y prioridad dentro de los conjuntos permitidos; formato válido de proyecto/asignado y tratamiento de `unassigned` según contrato. |
| Paginación | `page` entero desde 1; `pageSize` entero entre 1 y 100. Rechazar valores fuera de rango con 400, sin corregirlos silenciosamente. |
| Ordenación | `sort` en `createdAt` o `dueDate`; `order` en `asc` o `desc`. Valores no admitidos → 400. |
| Alta local y pertenencias | Evitar duplicados de `keycloak_sub` y pareja usuario-proyecto mediante unicidad y recuperación del registro existente ante carreras. No se devuelve un error de «nombre repetido». |

En edición general de tarea, `title` y `priority` son obligatorios; los opcionales sin valor se representan con `null` según el contrato PUT. No se admite cambiar `projectId`, estado o asignado mediante esa edición. El nombre y correo del perfil local proceden de identidad validada, no de un formulario de perfil modificable.

Los errores de entrada se devolverán como 400 con `ValidationProblemDetails`, nombres de campo del JSON y mensajes en `es-ES`. Autenticación y autorización mantienen sus códigos específicos; no se convierten en errores de validación por campo.

Se acuerda utilizar Problem Details y `ValidationProblemDetails`, con errores por campo en `es-ES` y un identificador de correlación para errores inesperados. No se exponen consultas SQL, credenciales ni trazas internas al cliente.

El contrato rechaza con 400 las propiedades no admitidas por el DTO, incluidas las protegidas. Los DTO nunca deben permitir modificar `created_by`, pertenencias o archivado mediante campos de la edición general.

## 9. Solicitudes duplicadas y reintentos

Para FR-018, el frontend bloquea nuevos envíos del formulario mientras la operación está pendiente. Se mantiene la decisión de no añadir claves de idempotencia ni deduplicación general de creaciones al backend en el MVP.

Por tanto, no debe configurarse un reintento automático indiscriminado de escrituras ante fallos de red: una petición de creación puede haberse confirmado en la base aunque se pierda su respuesta. Los reintentos de lecturas y los de operaciones concretas que sí sean idempotentes se configurarán de forma diferenciada.

La unicidad de usuarios locales y pertenencias evita duplicados estructurales durante sus altas concurrentes y sigue siendo necesaria. Es distinta de garantizar que repetir un `POST` de creación produzca un único recurso.

## 10. Configuración y operación

La API tendrá configuración por entorno para conexión a PostgreSQL, emisor de Keycloak, audiencia, orígenes CORS, zona de negocio y registros. Los valores locales acordados se recogen a continuación; su implementación y los comandos de uso se incorporarán a la guía de entorno en fase 1.

### Entorno local y ejecución de migraciones acordados

Por indicación del usuario se acuerda orquestar el desarrollo con **Aspire**, sustituyendo el flujo antes descrito con Docker Compose. Un AppHost en `infrastructure/TaskFlow.AppHost/` iniciará API .NET y SPA Vite como procesos del equipo, y PostgreSQL y Keycloak como contenedores mediante Docker. Se conservarán los puertos públicos acordados para OIDC y CORS. Las migraciones continuarán siendo un paso explícito separado: ni la API ni el arranque ordinario de AppHost las aplicarán automáticamente.

| Componente o configuración | Acuerdo local |
|---|---|
| SPA | `http://localhost:5173`; retorno de autenticación `http://localhost:5173/callback`. |
| API | `http://localhost:5080`; salud en `/health`. |
| Keycloak | `http://localhost:8080`; emisor `http://localhost:8080/realms/taskflow`. |
| PostgreSQL | Puerto local 5432; bases lógicas `taskflow` y `keycloak` con credenciales separadas. AppHost inyectará en la API la conexión de negocio. |
| Persistencia local | Volúmenes de los contenedores gestionados desde AppHost para conservar datos al reiniciar; borrar volúmenes será una operación explícita distinta. |
| Configuración de API | `ConnectionStrings__TaskFlow`, `Keycloak__Authority`, `Keycloak__Audience`, `Keycloak__Introspection__ClientId=taskflow-api`, `Keycloak__Introspection__ClientSecret` (secreto solo del servidor), `Cors__AllowedOrigins__0` y `BusinessTimeZone=Europe/Madrid`. Estos nombres deberán enlazarse al implementar la API. |
| Configuración del entorno .NET | `ASPNETCORE_ENVIRONMENT=Development` y URL local configurada para el puerto acordado. |
| Orquestación | Aspire AppHost gestiona recursos, dependencias, endpoints y panel de diagnóstico. Docker sigue siendo necesario para ejecutar los contenedores. |
| Migraciones en desarrollo | Con PostgreSQL disponible desde Aspire, aplicarlas explícitamente con EF Core antes de utilizar los recorridos de negocio. No se invoca `Migrate` ni `EnsureCreated` desde API o AppHost. Los reinicios ordinarios no aplican migraciones. |
| Datos iniciales | Semilla repetible de usuarios de demostración, sin duplicados y con `sub` coherentes con la importación del realm. |

HTTP y la configuración de desarrollo se limitan a este entorno local. Si posteriormente la API se ejecuta en un contenedor o cambian los orígenes, se revisarán el acceso al emisor, las URL de retorno y CORS. Los comandos concretos se añadirán cuando exista la solución y puedan verificarse.

Una **migración** es un cambio versionado del esquema: crear tablas, añadir una columna o modificar una restricción. EF Core genera archivos que se revisan y guardan en Git y registra cuáles se han aplicado a cada base. Generar el archivo y aplicarlo son pasos diferentes.

La primera migración creará el esquema inicial, aunque todavía no se haya añadido ninguna funcionalidad posterior. Las siguientes se generan cuando cambia el esquema: crear o eliminar tablas, añadir, renombrar o quitar columnas, cambiar tipos o longitudes, nulabilidad, índices, claves y restricciones. No se limitan a corregir atributos olvidados: también acompañan la evolución prevista del producto. Crear, editar o borrar proyectos, tareas o asignaciones son operaciones normales de datos y no requieren una migración por operación. Si no cambia el esquema ni se necesita una transformación versionada de datos, no se genera otra migración. Véase [EF Core: introducción a migraciones](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/).

EF Core no aplica migraciones al iniciar la API por defecto: eso requiere añadir una llamada explícita al arranque, como `Database.MigrateAsync()`. Se acuerda mantener la actualización del esquema como un paso separado en desarrollo. Con la base disponible desde Aspire, se ejecutará `dotnet ef database update` al preparar una base vacía o al recibir nuevas migraciones, antes de usar las operaciones de negocio. El proceso de API puede haber sido iniciado por AppHost; no ejecuta la actualización del esquema. Las rutas y argumentos de la herramienta se comprobarán al existir la solución. La herramienta consulta el historial de esa base y aplica solo las migraciones pendientes, sin repetir las ya aplicadas.

Separar ese paso no exige hacerlo siempre a mano: la preparación de la base temporal de pruebas puede automatizarlo. La automatización del despliegue se decidirá al preparar ese entorno; el acuerdo de desarrollo no selecciona todavía la estrategia de producción. Véase [EF Core: aplicación de migraciones](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying).

Una **transacción** agrupa operaciones que deben confirmarse o deshacerse juntas, por ejemplo crear un proyecto y su pertenencia inicial. No es una versión del esquema. Una migración puede ejecutarse dentro de una transacción cuando sus operaciones lo permiten; los dos conceptos tienen funciones distintas.

- Credenciales fuera de Git; valores de ejemplo sin secretos en la documentación de entorno.
- CORS limitado a los orígenes aprobados de la SPA. No sustituye autenticación ni autorización.
- En Docker, la URL de metadatos accesible por la API y el emisor público deben resolverse sin desactivar la validación de `iss`.
- Registros de operaciones y errores con identificadores de correlación, sin tokens completos ni contraseñas.
- `/health` comprobará la salud prevista en el contrato sin publicar detalles sensibles de conexión.
- Migraciones y datos iniciales tendrán un procedimiento explícito, reproducible y separado de la autenticación de usuarios.

La redacción de `deployment-aws.md` se mantiene aplazada hasta preparar el despliegue. Elegir Aspire para desarrollo no decide el despliegue AWS. El roadmap original todavía exige esa propuesta para fase 0 y menciona Docker Compose: se conserva sin editar hasta obtener aprobación del manager; las diferencias y el motivo están en [documentation-alignment.md](documentation-alignment.md).

## 11. Verificación prevista

No existen pruebas ejecutables todavía. Se acepta utilizar xUnit para reglas y pruebas de integración de ASP.NET Core mediante `WebApplicationFactory`, con PostgreSQL real cuando se comprueben relaciones, cascadas o consultas específicas.

Se acepta iniciar un PostgreSQL 18.6 temporal con Testcontainers para las pruebas de integración y aislar los datos de cada conjunto de pruebas. La preparación de esa base debe reproducir el esquema a partir de las migraciones, sin depender del arranque normal de la API; el código de preparación se implementará en fase 1. Docker debe estar disponible tanto en desarrollo como en el ejecutor de CI. La base temporal de pruebas no utilizará los datos de la base local del desarrollador.

Cobertura mínima:

- JWT ausente o inválido, emisor/audiencia/firma incorrectos, caducidad y roles no admitidos.
- Mismo access token admitido antes del logout y rechazado con 401 después de completar el cierre en Keycloak; repetir con revocación administrativa de la sesión.
- Introspección inactiva → 401; proveedor no disponible, tiempo de espera agotado, respuesta inválida o fallo de credencial confidencial → 503 sin ejecutar escrituras ni resolver el perfil local. Sin caché de resultados positivos entre peticiones; `/health` sin introspección.
- Recorridos A–E de la especificación nueva y permisos de `Admin`/`User` sobre activos y archivados.
- Visibilidad en detalles, listas, filtros y panel; «Mis tareas» sin aceptar identidad ajena.
- Creación de proyecto con pertenencia del autor y asignación con alta automática de miembro, sin cambios parciales.
- Altas simultáneas de `AppUser` y de pertenencia sin duplicados ni errores por carreras esperables.
- Borrado físico, cascadas y conservación del proyecto al dejar una tarea sin asignar.
- Rechazo íntegro del borrado de un usuario que sea el último miembro de algún proyecto, incluidos archivados; protección ante borrados concurrentes y posibilidad de eliminar el propio proyecto con un único miembro.
- Paginación, ordenación, filtros y recuentos sobre el conjunto completo autorizado.
- Vencimientos en `Europe/Madrid`: ayer, hoy, sin vencimiento y tarea completada.
- Migraciones desde base vacía y casos de FK compuesta con columnas opcionales.

La colección de Bruno documentada en el contrato permitirá comprobaciones manuales reproducibles. No sustituye las pruebas automatizadas. Se acepta que GitHub Actions ejecute restauración de paquetes, compilación y pruebas en las PR y en `main`. La comprobación de migraciones desde una base vacía se implementará como parte de la preparación de pruebas. La configuración se implementará en fase 1 y se ampliará con los componentes disponibles; todavía no existe un flujo que acredite esas comprobaciones.

## 12. Estado del diseño y trabajo pendiente

A fecha 2026-10-23, las decisiones propias del backend quedan definidas: cuatro capas y Controllers; acceso mediante repositorios específicos y guardado compartido; versiones de referencia; validadores y errores; consultas y paginación; modelo físico; última escritura gana; perfil local y su limitación de asignables; visibilidad con precedencia de 404; transacciones y bloqueos para operaciones simultáneas; entorno con Aspire, pruebas y aplicación separada de migraciones en desarrollo. Frontend confirma además los acuerdos de integración de §5 con una duración máxima de sesión SSO de 10 horas.

Este documento puede darse por completado en su definición técnica propia, con los pendientes externos señalados a continuación. Esto no sustituye la revisión de la propuesta conjunta ni acredita implementación o cierre formal de toda la fase 0.

### Estado de la coordinación externa

| Con quién | Estado |
|---|---|
| Frontend | Confirmados todos los puntos de §5, con sesión SSO máxima de 10 horas. Falta implementar y comprobar la integración, sin nuevas preguntas pendientes de esta revisión. |
| Manager | Revisión formal y autorización para actualizar sus documentos originales. La decisión técnica de revocación ya está cerrada por indicación del usuario; no se presenta como aprobación del manager. |

### Coherencia documental y alcance de la actualización

Se sincronizan los documentos del equipo: este backend, modelo de datos, contrato de API, autenticación y frontend. La guía de entorno incorpora Aspire y las normas de contribución enlazan el seguimiento.

Por indicación del usuario, `system-overview.md`, `functional-requirements.md` y `roadmap.md` permanecen sin editar hasta obtener la aprobación del manager. El [registro de coherencia](documentation-alignment.md) recoge, por archivo y apartado, las modificaciones pendientes y por qué se han aplazado. Las diferencias conocidas no se presentan como decisiones nuevas ni como aprobación formal de los documentos originales.

### Implementación y comprobaciones posteriores al análisis

Corresponden al trabajo de implementación, no a nuevas decisiones previas sobre arquitectura:

- Crear la solución y fijar las versiones en los archivos de proyectos, herramientas e imágenes; comprobar la integración de la combinación elegida.
- Implementar servicios, contratos, repositorios y contexto; generar y revisar la primera migración, incluido el `SET NULL` parcial y la reconstrucción desde una base vacía.
- Implementar alta y sincronización del perfil, transacciones y bloqueos ordenados, y el procedimiento controlado de mantenimiento para eliminar usuarios locales. Verificar carreras, cascadas y protección del último miembro.
- Crear AppHost de Aspire, configuración, datos de demostración y comandos comprobados de arranque y actualización del esquema; mantener fijo el emisor público y separar las credenciales de cada base.
- Implementar pruebas y CI, generar OpenAPI y revisar los índices con las consultas reales.

La pertenencia se conserva al desasignar, reasignar o eliminar tareas. No se añaden endpoints de retirada de miembros ni de eliminación de usuarios al MVP. Última escritura gana sigue vigente; la detección de conflictos de edición queda como mejora posterior en el [backlog](../backlog.md).

Por indicación del usuario, los ADR se completarán conforme avance el trabajo. Los acuerdos actuales quedan recogidos en este documento y en el modelo de datos. El despliegue permanece aplazado hasta preparar ese entorno.

## 13. Referencias técnicas de la revisión

- [ASP.NET Core: autenticación JWT Bearer](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication).
- [ASP.NET Core: tratamiento de errores de API](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling-api).
- [EF Core: claves foráneas y principales](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/foreign-and-principal-keys), incluido el orden de las propiedades en claves compuestas.
- [PostgreSQL 15: restricciones y acciones de claves foráneas](https://www.postgresql.org/docs/15/ddl-constraints.html), incluido `SET NULL` sobre un subconjunto de columnas.
- [RFC 9110, §13.1.1: If-Match](https://www.rfc-editor.org/rfc/rfc9110.html#name-if-match).
- [ASP.NET Core: validación de modelos](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation).
- [EF Core: transacciones](https://learn.microsoft.com/en-us/ef/core/saving/transactions).
- [Testcontainers para .NET: PostgreSQL](https://dotnet.testcontainers.org/modules/postgres/).

