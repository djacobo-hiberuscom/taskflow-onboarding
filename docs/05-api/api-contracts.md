# TaskFlow — Contrato de la API

> **Responsable:** David — backend (acordado con frontend)
>
> **Estado:** contrato inicial actualizado con los acuerdos del análisis; pendiente de revisión conjunta de fase 0. No hay endpoints implementados.
>
> **Coherencia:** los documentos del manager se conservan sin cambios hasta su aprobación; discrepancias y seguimiento en [documentation-alignment.md](../03-architecture/documentation-alignment.md).
>
> **Fuentes:** [requisitos funcionales](../02-requirements/functional-requirements.md) · [visión general del sistema §7](../03-architecture/system-overview.md) · [modelo de datos](../03-architecture/database.md) · [autenticación](../03-architecture/authentication.md)

## 1. Propósito

Este documento define el contrato inicial de trabajo de la API: rutas, datos de entrada y salida, validaciones, códigos HTTP y errores. En la implementación se generará OpenAPI desde los Controllers y se versionará en `docs/05-api/`. Hasta entonces, el frontend puede usar simulaciones tipadas de este contrato, que se retirarán antes de la entrega.

Cualquier cambio posterior se acuerda en la coordinación de contratos y se actualiza aquí en la misma PR que lo implementa.

**Comprobación manual con Bruno.** La API se comprobará con [Bruno](https://www.usebruno.com/): una colección versionada en el repositorio con una petición por endpoint de §5 y entornos (local, AWS) que no contienen secretos; los tokens se obtienen en tiempo de ejecución. La ruta de la colección se fijará al crearla.

## 2. Convenciones generales

| Aspecto | Acuerdo |
|---|---|
| Prefijo | `/api/v1` |
| Formato | JSON (`application/json`); errores como `application/problem+json` |
| Nombres de campos | `camelCase` en JSON |
| Autenticación | `Authorization: Bearer <access token>` en todas las rutas salvo `/health`; validación JWT e introspección en Keycloak por petición protegida |
| Identificadores | UUID serializados como cadenas; el frontend los trata como valores opacos. Formato inválido → 400; recurso válido pero inexistente o no visible → 404 |
| Instantes | ISO 8601 en UTC con `Z` (`"2026-10-22T08:15:00Z"`) |
| Fecha de vencimiento | Fecha de calendario `YYYY-MM-DD` en el campo `dueDate` (columna `due_date`), sin hora ni zona |
| Estados | `"ToDo"`, `"InProgress"`, `"Done"` → etiquetas «Pendiente», «En curso», «Completada» |
| Prioridades | `"Low"`, `"Medium"`, `"High"` → etiquetas «Baja», «Media», «Alta» |
| Campos opcionales | Se envían como `null` cuando no tienen valor; las cadenas se recortan y una cadena vacía en un campo opcional se guarda como `null` |
| Creación | `201 Created` con cabecera `Location` y el recurso en el cuerpo |
| Acciones sin contenido | `204 No Content` |

Las etiquetas en español solo existen en la interfaz; la API usa siempre los valores técnicos.

## 3. Errores

Se acuerda utilizar Problem Details ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)) para todos los errores.

| Código | Cuándo | Cuerpo |
|---|---|---|
| 400 | Datos con formato o valores inválidos | `ValidationProblemDetails` con `errors` por campo |
| 401 | Sin token, token inválido, caducado o inactivo según introspección | Sin cuerpo o Problem Details mínimo; cabecera `WWW-Authenticate` |
| 403 | Token válido sin rol admitido, o `User` invoca una operación de `Admin` sobre un recurso visible | Problem Details |
| 404 | Recurso inexistente **o no visible** para `User` | Problem Details, idéntico en ambos casos |
| 500 | Error inesperado | Problem Details sin detalles internos; `traceId` para correlacionar con los registros |
| 503 | Fallo de salud o imposibilidad de comprobar introspección (indisponibilidad, tiempo de espera agotado, respuesta inválida o fallo de credencial del cliente servidor) | Problem Details sin detalles internos; si falla introspección, no se ejecuta la operación de negocio |

Todas las rutas protegidas pueden devolver 503 si no se puede comprobar el estado del token. Un resultado inactivo devuelve 401, no 503. `/health` es anónimo y no requiere introspección. Véase [authentication.md §5](../03-architecture/authentication.md).

Ejemplo de validación:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "name": ["El nombre es obligatorio."],
    "priority": ["Valor no admitido."]
  },
  "traceId": "00-…"
}
```

Los mensajes de `errors` se devuelven en español (`es-ES`) y sus claves corresponden a los campos JSON en `camelCase`. Las escrituras exigen JSON válido y un objeto no nulo; las propiedades no admitidas por el DTO se rechazan con 400. Estados y prioridades solo admiten los códigos de texto exactos de §2; se rechazan números, `null` y valores desconocidos cuando el campo es obligatorio.

Una vez autenticado el solicitante y comprobado que tiene un rol admitido, las operaciones sobre un recurso concreto comprueban primero su visibilidad: inexistente o no visible → 404 con el mismo cuerpo; solo para un recurso visible se comprueba el permiso administrativo y puede devolverse 403. Las listas vacías y el panel sin datos devuelven 200.

## 4. Representaciones

### 4.1. Usuario

```json
{ "id": "11111111-1111-4111-8111-111111111111", "displayName": "Usuario B" }
```

`UserSummary` se usa en miembros, asignados y lista de usuarios asignables. No expone `email` ni `keycloak_sub` a otros usuarios. La lista se obtiene de referencias locales; se acepta para el MVP que una identidad deshabilitada o eliminada en Keycloak pueda seguir apareciendo, al no haber sincronización de actividad.

### 4.2. Perfil actual

```json
{
  "id": "11111111-1111-4111-8111-111111111111",
  "displayName": "Usuario A",
  "email": "usuario.a@example.com",
  "roles": ["User"]
}
```

`GET /api/v1/me` sincroniza nombre y correo desde el token validado según [authentication.md §6](../03-architecture/authentication.md). La SPA lo consulta después del login y tras renovar el token. Los roles proceden del token de cada petición y no se persisten.

### 4.3. Proyecto

```json
{
  "id": "33333333-3333-4333-8333-333333333333",
  "name": "Rediseño del sitio web",
  "description": "Nueva web corporativa",
  "createdBy": { "id": "11111111-1111-4111-8111-111111111111", "displayName": "Usuario A" },
  "archivedAt": null,
  "createdAt": "2026-10-22T08:15:00Z",
  "updatedAt": "2026-10-22T08:15:00Z"
}
```

El detalle (`ProjectDetail`) añade `"members": [UserSummary, …]`. `createdBy` puede ser `null` si se eliminó el autor.

### 4.4. Tarea

```json
{
  "id": "44444444-4444-4444-8444-444444444444",
  "projectId": "33333333-3333-4333-8333-333333333333",
  "projectName": "Rediseño del sitio web",
  "title": "Construir la página de inicio",
  "description": null,
  "status": "InProgress",
  "priority": "High",
  "dueDate": "2026-11-15",
  "assignee": { "id": "22222222-2222-4222-8222-222222222222", "displayName": "Usuario B" },
  "createdAt": "2026-10-22T08:20:00Z",
  "updatedAt": "2026-10-23T09:00:00Z"
}
```

No se incluye `isOverdue`: el frontend lo calcula con `status`, `dueDate` y la fecha de negocio de `GET /api/v1/dashboard` o de la configuración acordada ([database.md §6](../03-architecture/database.md)).

## 5. Endpoints

Leyenda de permisos: **A** = `Admin`; **U** = `User`; **U·m** = `User` miembro de un proyecto activo.

### 5.1. Sistema e identidad

| Método y ruta | Permiso | Descripción | Respuestas | Requisito |
|---|---|---|---|---|
| `GET /health` | Anónimo | Comprobación de salud (API y conexión a base de datos) | 200, 503 | — |
| `GET /api/v1/me` | A, U | Perfil y roles del usuario actual | 200, 401, 403, 503 | FR-003 |
| `GET /api/v1/users/assignable` | A, U | Lista de usuarios asignables (`UserSummary[]`), ordenada por `displayName` | 200, 401, 403, 503 | FR-013 |

### 5.2. Proyectos

| Método y ruta | Permiso | Descripción | Respuestas | Requisito |
|---|---|---|---|---|
| `GET /api/v1/projects` | A, U | Proyectos activos visibles. `?archived=true` lista los archivados (solo A) | 200, 400, 401, 403, 503 | FR-004, FR-022 |
| `GET /api/v1/projects/{projectId}` | A, U·m | Detalle con miembros. A puede abrir un archivado | 200, 400, 401, 403, 404, 503 | FR-005 |
| `POST /api/v1/projects` | A, U | Crea un proyecto; el autor queda como creador y miembro | 201, 400, 401, 403, 503 | FR-006, FR-023 |
| `PUT /api/v1/projects/{projectId}` | A, U·m | Edita `name` y `description` | 200, 400, 401, 403, 404, 503 | FR-007 |
| `POST /api/v1/projects/{projectId}/archive` | A | Archiva. Idempotente | 204, 400, 401, 403, 404, 503 | FR-008 |
| `POST /api/v1/projects/{projectId}/restore` | A | Restaura un archivado. Idempotente | 204, 400, 401, 403, 404, 503 | FR-022 |
| `DELETE /api/v1/projects/{projectId}` | A | Elimina el proyecto, sus tareas y pertenencias | 204, 400, 401, 403, 404, 503 | FR-021 |

Para `archive`, `restore` y `DELETE`, `User` recibe 403 si el proyecto es visible y 404 si no lo es. Solicitar `GET /projects?archived=true` devuelve 403 a `User`: es una lista administrativa sin recurso individual cuya existencia ocultar.

**Cuerpo de creación y edición**

```json
{ "name": "Rediseño del sitio web", "description": "Nueva web corporativa" }
```

| Campo | Reglas |
|---|---|
| `name` | Obligatorio; entre 1 y 150 caracteres tras recortar espacios. Se permiten nombres repetidos |
| `description` | Opcional; hasta 5.000 caracteres tras recortar; vacío equivale a `null` |

Solo se admiten `name` y `description`. Campos como `createdBy`, `archivedAt`, `members` o cualquier propiedad no admitida se rechazan con 400. El autor se obtiene del usuario autenticado. En edición, `name` es obligatorio y una descripción omitida o sin valor queda a `null`.

### 5.3. Tareas

| Método y ruta | Permiso | Descripción | Respuestas | Requisito |
|---|---|---|---|---|
| `GET /api/v1/tasks` | A, U | Tareas visibles con filtros y ordenación (§6) | 200, 400, 401, 403, 404, 503 | FR-010, FR-015 |
| `GET /api/v1/tasks/{taskId}` | A, U·m | Detalle de una tarea | 200, 400, 401, 403, 404, 503 | FR-010 |
| `POST /api/v1/projects/{projectId}/tasks` | A, U·m | Crea una tarea en el proyecto | 201, 400, 401, 403, 404, 503 | FR-009 |
| `PUT /api/v1/tasks/{taskId}` | A, U·m | Edita `title`, `description`, `priority`, `dueDate` | 200, 400, 401, 403, 404, 503 | FR-011 |
| `PUT /api/v1/tasks/{taskId}/assignee` | A, U·m | Asigna, reasigna o desasigna; alta automática de pertenencia | 200, 400, 401, 403, 404, 503 | FR-013, FR-023 |
| `PUT /api/v1/tasks/{taskId}/status` | A, U·m | Cambia el estado | 200, 400, 401, 403, 404, 503 | FR-014 |
| `DELETE /api/v1/tasks/{taskId}` | A, U·m | Elimina físicamente la tarea | 204, 400, 401, 403, 404, 503 | FR-012 |
| `GET /api/v1/me/tasks` | A, U | Tareas asignadas al usuario actual en proyectos activos; admite los mismos filtros y ordenación salvo `assigneeId` | 200, 400, 401, 403, 404, 503 | FR-016 |

**Crear tarea**

```json
{
  "title": "Construir la página de inicio",
  "description": null,
  "status": "ToDo",
  "priority": "High",
  "dueDate": "2026-11-15",
  "assigneeId": "22222222-2222-4222-8222-222222222222"
}
```

| Campo | Reglas |
|---|---|
| `title` | Obligatorio; entre 1 y 200 caracteres tras recortar. Se permiten títulos repetidos |
| `description` | Opcional; hasta 5.000 caracteres tras recortar; vacío equivale a `null` |
| `status` | Puede omitirse en creación y se aplica `ToDo`; si se envía, debe ser un código de §2. `null` y números son inválidos; el estado persistido nunca es nulo |
| `priority` | Obligatorio; solo códigos de texto de §2, sin números ni `null` |
| `dueDate` | Opcional; fecha `YYYY-MM-DD` válida. Se admiten fechas pasadas |
| `assigneeId` | Opcional: `null` o UUID de un usuario local existente. Si no es miembro, se crea la pertenencia en la misma transacción; usuario desconocido → 400 en el campo |

**Editar tarea** — `{ "title", "description", "priority", "dueDate" }`, con `title` y `priority` obligatorios y las mismas reglas de campos. Los opcionales omitidos o sin valor quedan a `null` en este PUT. No admite `projectId`, `status` ni `assigneeId`; enviarlos produce 400. La tarea conserva su proyecto y las otras dos propiedades tienen sus operaciones específicas.

**Asignar** — `{ "assigneeId": "22222222-2222-4222-8222-222222222222" }` o `{ "assigneeId": null }` para desasignar. La propiedad `assigneeId` debe estar presente: `{}` es inválido. Un UUID mal formado o usuario local desconocido responde 400 con error en `assigneeId`. La tarea tiene como máximo un asignado; reasignar lo sustituye. La respuesta devuelve la tarea actualizada.

**Cambiar estado** — `{ "status": "Done" }`. `status` es obligatorio y solo admite los códigos de §2; no se admiten otras propiedades. Se admite cualquier transición. Devuelve la tarea actualizada.

### 5.4. Panel

| Método y ruta | Permiso | Descripción | Respuestas | Requisito |
|---|---|---|---|---|
| `GET /api/v1/dashboard` | A, U | Recuentos con la visibilidad del usuario | 200, 401, 403, 503 | FR-017 |

```json
{
  "activeProjects": 3,
  "openTasks": 12,
  "completedTasks": 7,
  "overdueTasks": 2,
  "businessDate": "2026-10-22",
  "businessTimeZone": "Europe/Madrid"
}
```

`overdueTasks` lo calcula la API con la fecha actual en `Europe/Madrid`. `businessDate` y `businessTimeZone` permiten al frontend comprobar que calcula `isOverdue` por tarea con la misma referencia que el panel ([frontend.md §5](../03-architecture/frontend.md)).

## 6. Filtros, ordenación y paginación de tareas

Parámetros de `GET /api/v1/tasks` y `GET /api/v1/me/tasks`:

| Parámetro | Valores | Notas |
|---|---|---|
| `projectId` | UUID | Si el proyecto no es visible para `User`, se responde 404. `Admin` puede indicar un proyecto archivado |
| `status` | `ToDo`, `InProgress`, `Done`; repetible (`?status=ToDo&status=InProgress`) | Sin valor = todos |
| `priority` | `Low`, `Medium`, `High`; repetible | Sin valor = todas |
| `assigneeId` | UUID o `unassigned` | Solo en `/tasks` |
| `sort` | `dueDate`, `createdAt` | Único campo de ordenación admitido en el MVP |
| `order` | `asc`, `desc` | |

Sin `projectId`, `/tasks` devuelve solo tareas de proyectos activos visibles, también para `Admin`. Un valor no admitido en cualquier parámetro responde 400.

**Ordenación acordada:**

| Aspecto | Acuerdo |
|---|---|
| Campos admitidos | `dueDate` y `createdAt` |
| Orden predeterminado | `sort=createdAt&order=desc` (las más recientes primero) |
| Dirección predeterminada con `sort=dueDate` | `asc` (vencimientos más próximos primero) |
| Tareas sin vencimiento | Siempre al final, en ambas direcciones (`NULLS LAST`) |
| Desempate por vencimiento | `createdAt desc` y después `id asc` |
| Desempate al ordenar por creación | `id asc` |

**Paginación (decidida).** `GET /api/v1/projects` (activos y archivados), `GET /api/v1/tasks` y `GET /api/v1/me/tasks` están paginados:

| Parámetro | Valores | Predeterminado |
|---|---|---|
| `page` | Entero ≥ 1 | 1 |
| `pageSize` | Entero entre 1 y 100 | 20 |

Respuesta:

```json
{ "items": [ … ], "page": 1, "pageSize": 20, "totalCount": 57 }
```

Una página posterior a la última devuelve `items` vacío con el `totalCount` real, calculado sobre todo el conjunto filtrado y autorizado antes de paginar. Valores no válidos de `page` o `pageSize` → 400, sin corregirlos silenciosamente. Los proyectos se ordenan por `createdAt desc` e `id asc`. La API ordena en PostgreSQL antes de paginar; el desempate es determinista para un mismo conjunto de datos, aunque otras operaciones pueden cambiar ese conjunto entre peticiones. Los recuentos del panel no dependen de la paginación porque los calcula la API. `GET /api/v1/users/assignable` y los miembros del detalle de proyecto no se paginan en el MVP.

## 7. Matriz de códigos por situación

| Situación | Respuesta |
|---|---|
| `User` lista proyectos | 200 con solo sus proyectos activos |
| `User` abre un proyecto del que no es miembro | 404 |
| `User` abre una tarea de un proyecto archivado del que es miembro | 404 |
| `User` miembro intenta archivar su proyecto activo | 403 |
| `User` intenta archivar, restaurar o eliminar un proyecto no visible | 404 |
| `User` solicita la lista de archivados | 403 |
| `User` crea tarea en proyecto ajeno | 404 |
| `User` asigna una tarea a un usuario no miembro | 200; el usuario pasa a ser miembro |
| Asignación a un usuario inexistente | 400 |
| `Admin` crea tarea en proyecto archivado | 201 |
| Proyecto o tarea inexistente | 404 |
| Token sin rol `Admin` ni `User` | 403 |

## 8. Recorridos y rutas implicadas

| Recorrido | Llamadas principales |
|---|---|
| A — `User` crea y asigna | `POST /projects` → `POST /projects/{id}/tasks` → `GET /users/assignable` → `PUT /tasks/{id}/assignee` → `GET /projects/{id}` |
| B — `User` asignado colabora | `GET /projects` → `GET /me/tasks` → `PUT /tasks/{id}/status` → `POST /projects/{id}/tasks` → `PUT /tasks/{id}` → `GET /dashboard` → `POST /projects/{id}/archive` (403) |
| C — Visibilidad y validación | `GET /projects/{id}` (404) → `GET /tasks/{id}` (404) → `POST /projects` vacío (400) → `POST /projects/{inexistente}/tasks` (404) |
| E — `Admin` | `GET /projects` → `POST /projects/{id}/archive` → `GET /projects?archived=true` → `POST /projects/{id}/restore` → `DELETE /projects/{id}` |

## 9. Concurrencia y reintentos

Se acuerda **última escritura gana** para el MVP. No se devuelven versiones de edición ni se exigen `ETag` o `If-Match`; no se detectan conflictos de versiones ni se responde 409 por este motivo. La mejora se recoge en el [backlog](../backlog.md).

Las operaciones relacionadas se ejecutan con las transacciones y bloqueos de [backend.md §6](../03-architecture/backend.md). Si un archivado o borrado termina antes, una escritura posterior de `User` vuelve a comprobar el estado y puede devolver 404. Esto protege integridad y permisos; no compara versiones de formularios.

FR-018 se cubre en frontend desactivando los envíos pendientes. No se añaden claves de idempotencia ni deduplicación general de creaciones en el backend. Las escrituras ya enviadas no se reintentan automáticamente ante errores de red; los reintentos de lecturas se configuran por separado.

## 10. Estado de los acuerdos y seguimiento

Quedan concretados UUID, límites de texto, estado inicial, códigos de estado/prioridad, idioma `es-ES`, zona `Europe/Madrid`, ordenación, paginación, última escritura gana, rechazo de campos desconocidos y generación de OpenAPI desde Controllers.

La revocación queda acordada en [authentication.md §5](../03-architecture/authentication.md): validación JWT más introspección en cada petición protegida, sin caché de resultados positivos; token inactivo → 401 y fallo de comprobación → 503 sin ejecutar la operación. Quedan pendientes su implementación y pruebas.

Los documentos originales del manager no se modifican hasta su aprobación. El [registro de coherencia](../03-architecture/documentation-alignment.md) identifica las aclaraciones pendientes en requisitos y visión general; entre ellas, FR-009 debe distinguir un estado persistido obligatorio de poder omitirlo en el cuerpo de creación.
