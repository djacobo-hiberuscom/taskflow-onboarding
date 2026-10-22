# TaskFlow — Contrato de la API

> **Responsable:** David — backend (acordado con frontend)
>
> **Estado:** borrador de fase 0; **propuesta pendiente de acuerdo** entre backend y frontend y de aprobación del responsable técnico. No hay endpoints implementados.
>
> **Fuentes:** [requisitos funcionales](../02-requirements/functional-requirements.md) · [visión general del sistema §7](../03-architecture/system-overview.md) · [modelo de datos](../03-architecture/database.md) · [autenticación](../03-architecture/authentication.md)

## 1. Propósito

Este documento propone las rutas, datos de entrada y salida, validaciones, códigos HTTP y errores de la API de TaskFlow, para que backend y frontend trabajen en paralelo. Cuando se acuerde, se traducirá a un documento OpenAPI versionado en el repositorio; mientras tanto, el frontend puede usar simulaciones tipadas de este contrato, que se retirarán antes de la entrega.

Cualquier cambio posterior se acuerda en la coordinación de contratos y se actualiza aquí en la misma PR que lo implementa.

**Comprobación manual con Bruno.** La API se comprobará con [Bruno](https://www.usebruno.com/): una colección versionada en el repositorio con una petición por endpoint de §5 y entornos (local, AWS) que no contienen secretos; los tokens se obtienen en tiempo de ejecución. La ruta de la colección se fijará al crearla.

## 2. Convenciones generales

| Aspecto | Propuesta |
|---|---|
| Prefijo | `/api/v1` |
| Formato | JSON (`application/json`); errores como `application/problem+json` |
| Nombres de campos | `camelCase` en JSON |
| Autenticación | `Authorization: Bearer <access token>` en todas las rutas salvo `/health` |
| Identificadores | Cadenas opacas para el cliente (`"id": "…"`); el tipo físico (UUID o entero) se decide en [database.md](../03-architecture/database.md) |
| Instantes | ISO 8601 en UTC con `Z` (`"2026-10-22T08:15:00Z"`) |
| Fecha de vencimiento | Fecha de calendario `YYYY-MM-DD` en el campo `dueDate` (columna `end_at`), sin hora ni zona |
| Estados | `"ToDo"`, `"InProgress"`, `"Done"` → etiquetas «Pendiente», «En curso», «Completada» |
| Prioridades | `"Low"`, `"Medium"`, `"High"` → etiquetas «Baja», «Media», «Alta» |
| Campos opcionales | Se envían como `null` cuando no tienen valor; las cadenas se recortan y una cadena vacía en un campo opcional se guarda como `null` |
| Creación | `201 Created` con cabecera `Location` y el recurso en el cuerpo |
| Acciones sin contenido | `204 No Content` |

Las etiquetas en español solo existen en la interfaz; la API usa siempre los valores técnicos.

## 3. Errores

Se propone Problem Details ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)) para todos los errores.

| Código | Cuándo | Cuerpo |
|---|---|---|
| 400 | Datos con formato o valores inválidos | `ValidationProblemDetails` con `errors` por campo |
| 401 | Sin token, token inválido o caducado | Sin cuerpo o Problem Details mínimo; cabecera `WWW-Authenticate` |
| 403 | Token válido sin rol admitido, o `User` invoca una operación de `Admin` sobre un recurso visible | Problem Details |
| 404 | Recurso inexistente **o no visible** para `User` | Problem Details, idéntico en ambos casos |
| 409 | Conflicto de concurrencia, si se adopta (§9) | Problem Details |
| 500 | Error inesperado | Problem Details sin detalles internos; `traceId` para correlacionar con los registros |

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

Los mensajes de `errors` se devuelven en español (`es-ES`) y la interfaz los muestra tal cual.

## 4. Representaciones

### 4.1. Usuario

```json
{ "id": "u-1", "displayName": "Usuario B" }
```

`UserSummary` se usa en miembros, asignados y lista de usuarios asignables. No expone `email` ni `keycloak_sub` a otros usuarios.

### 4.2. Perfil actual

```json
{
  "id": "u-1",
  "displayName": "Usuario A",
  "email": "usuario.a@example.com",
  "roles": ["User"]
}
```

### 4.3. Proyecto

```json
{
  "id": "p-1",
  "name": "Rediseño del sitio web",
  "description": "Nueva web corporativa",
  "createdBy": { "id": "u-1", "displayName": "Usuario A" },
  "archivedAt": null,
  "createdAt": "2026-10-22T08:15:00Z",
  "updatedAt": "2026-10-22T08:15:00Z"
}
```

El detalle (`ProjectDetail`) añade `"members": [UserSummary, …]`. `createdBy` puede ser `null` si se eliminó el autor.

### 4.4. Tarea

```json
{
  "id": "t-1",
  "projectId": "p-1",
  "projectName": "Rediseño del sitio web",
  "title": "Construir la página de inicio",
  "description": null,
  "status": "InProgress",
  "priority": "High",
  "dueDate": "2026-11-15",
  "assignee": { "id": "u-2", "displayName": "Usuario B" },
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
| `GET /api/v1/me` | A, U | Perfil y roles del usuario actual | 200, 401, 403 | FR-003 |
| `GET /api/v1/users/assignable` | A, U | Lista de usuarios asignables (`UserSummary[]`), ordenada por `displayName` | 200, 401, 403 | FR-013 |

### 5.2. Proyectos

| Método y ruta | Permiso | Descripción | Respuestas | Requisito |
|---|---|---|---|---|
| `GET /api/v1/projects` | A, U | Proyectos activos visibles. `?archived=true` lista los archivados (solo A) | 200, 401, 403 | FR-004, FR-022 |
| `GET /api/v1/projects/{projectId}` | A, U·m | Detalle con miembros. A puede abrir un archivado | 200, 401, 403, 404 | FR-005 |
| `POST /api/v1/projects` | A, U | Crea un proyecto; el autor queda como creador y miembro | 201, 400, 401, 403 | FR-006, FR-023 |
| `PUT /api/v1/projects/{projectId}` | A, U·m | Edita `name` y `description` | 200, 400, 401, 403, 404 | FR-007 |
| `POST /api/v1/projects/{projectId}/archive` | A | Archiva. Idempotente | 204, 401, 403, 404 | FR-008 |
| `POST /api/v1/projects/{projectId}/restore` | A | Restaura un archivado. Idempotente | 204, 401, 403, 404 | FR-022 |
| `DELETE /api/v1/projects/{projectId}` | A | Elimina el proyecto, sus tareas y pertenencias | 204, 401, 403, 404 | FR-021 |

`User` recibe 403 en `archive`, `restore`, `DELETE` y `?archived=true` si el proyecto le es visible, y 404 si no lo es.

**Cuerpo de creación y edición**

```json
{ "name": "Rediseño del sitio web", "description": "Nueva web corporativa" }
```

| Campo | Reglas |
|---|---|
| `name` | Obligatorio; tras recortar espacios, no vacío; longitud máxima pendiente (§10) |
| `description` | Opcional; longitud máxima pendiente |

Campos como `createdBy`, `archivedAt` o `members` se ignoran si llegan en el cuerpo; se propone rechazar propiedades desconocidas con 400 para detectar errores del cliente.

### 5.3. Tareas

| Método y ruta | Permiso | Descripción | Respuestas | Requisito |
|---|---|---|---|---|
| `GET /api/v1/tasks` | A, U | Tareas visibles con filtros y ordenación (§6) | 200, 400, 401, 403 | FR-010, FR-015 |
| `GET /api/v1/tasks/{taskId}` | A, U·m | Detalle de una tarea | 200, 401, 403, 404 | FR-010 |
| `POST /api/v1/projects/{projectId}/tasks` | A, U·m | Crea una tarea en el proyecto | 201, 400, 401, 403, 404 | FR-009 |
| `PUT /api/v1/tasks/{taskId}` | A, U·m | Edita `title`, `description`, `priority`, `dueDate` | 200, 400, 401, 403, 404 | FR-011 |
| `PUT /api/v1/tasks/{taskId}/assignee` | A, U·m | Asigna, reasigna o desasigna; alta automática de pertenencia | 200, 400, 401, 403, 404 | FR-013, FR-023 |
| `PUT /api/v1/tasks/{taskId}/status` | A, U·m | Cambia el estado | 200, 400, 401, 403, 404 | FR-014 |
| `DELETE /api/v1/tasks/{taskId}` | A, U·m | Elimina físicamente la tarea | 204, 401, 403, 404 | FR-012 |
| `GET /api/v1/me/tasks` | A, U | Tareas asignadas al usuario actual en proyectos activos; admite los mismos filtros y ordenación salvo `assigneeId` | 200, 400, 401, 403 | FR-016 |

**Crear tarea**

```json
{
  "title": "Construir la página de inicio",
  "description": null,
  "status": "ToDo",
  "priority": "High",
  "dueDate": "2026-11-15",
  "assigneeId": "u-2"
}
```

| Campo | Reglas |
|---|---|
| `title` | Obligatorio; no vacío tras recortar |
| `description` | Opcional |
| `status` | Opcional; si se omite, `ToDo` (propuesta). Solo valores de §2 |
| `priority` | Obligatorio; solo valores de §2 |
| `dueDate` | Opcional; fecha `YYYY-MM-DD` válida. Se admiten fechas pasadas |
| `assigneeId` | Opcional; debe existir en la lista de usuarios asignables; si no es miembro, pasa a serlo |

**Editar tarea** — `{ "title", "description", "priority", "dueDate" }`, mismas reglas. No admite `projectId`, `status` ni `assigneeId`: cada uno tiene su operación.

**Asignar** — `{ "assigneeId": "u-2" }` o `{ "assigneeId": null }` para desasignar. Un `assigneeId` desconocido responde 400 con error en `assigneeId`. La respuesta devuelve la tarea actualizada.

**Cambiar estado** — `{ "status": "Done" }`. Se admite cualquier transición. Devuelve la tarea actualizada.

### 5.4. Panel

| Método y ruta | Permiso | Descripción | Respuestas | Requisito |
|---|---|---|---|---|
| `GET /api/v1/dashboard` | A, U | Recuentos con la visibilidad del usuario | 200, 401, 403 | FR-017 |

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
| `projectId` | Identificador | Si el proyecto no es visible para `User`, se responde 404. `Admin` puede indicar un proyecto archivado |
| `status` | `ToDo`, `InProgress`, `Done`; repetible (`?status=ToDo&status=InProgress`) | Sin valor = todos |
| `priority` | `Low`, `Medium`, `High`; repetible | Sin valor = todas |
| `assigneeId` | Identificador o `unassigned` | Solo en `/tasks` |
| `sort` | `dueDate`, `createdAt` | Único campo de ordenación admitido en el MVP |
| `order` | `asc`, `desc` | |

Sin `projectId`, `/tasks` devuelve solo tareas de proyectos activos visibles, también para `Admin`. Un valor no admitido en cualquier parámetro responde 400.

**Propuesta de ordenación** (para acordar):

| Aspecto | Propuesta |
|---|---|
| Campos admitidos | `dueDate` y `createdAt` |
| Orden predeterminado | `sort=createdAt&order=desc` (las más recientes primero) |
| Dirección predeterminada con `sort=dueDate` | `asc` (vencimientos más próximos primero) |
| Tareas sin vencimiento | Siempre al final, en ambas direcciones (`NULLS LAST`) |
| Desempate | `createdAt desc` y después `id`, para que el orden sea estable entre páginas |

**Paginación (decidida).** `GET /api/v1/projects` (activos y archivados), `GET /api/v1/tasks` y `GET /api/v1/me/tasks` están paginados:

| Parámetro | Valores | Predeterminado |
|---|---|---|
| `page` | Entero ≥ 1 | 1 |
| `pageSize` | Entero entre 1 y un máximo acordado (propuesta: 100) | 20 (propuesta) |

Respuesta:

```json
{ "items": [ … ], "page": 1, "pageSize": 20, "totalCount": 57 }
```

Una página fuera de rango devuelve `items` vacío con el `totalCount` real. Valores no válidos de `page` o `pageSize` → 400. El desempate por `id` de la ordenación garantiza un orden estable entre páginas. Los recuentos del panel no dependen de la paginación porque los calcula la API. `GET /api/v1/users/assignable` y los miembros del detalle de proyecto no se paginan en el MVP.

## 7. Matriz de códigos por situación

| Situación | Respuesta |
|---|---|
| `User` lista proyectos | 200 con solo sus proyectos activos |
| `User` abre un proyecto del que no es miembro | 404 |
| `User` abre una tarea de un proyecto archivado del que es miembro | 404 |
| `User` miembro intenta archivar | 403 |
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

## 9. Concurrencia

Pendiente. Opción propuesta si se adopta: la API devuelve una versión (`ETag` o campo `version`) y las operaciones `PUT` la exigen en `If-Match`; si no coincide, 409. En el MVP puede aceptarse «última escritura gana» si se documenta.

## 10. Decisiones pendientes

| Decisión | Propuesta actual |
|---|---|
| Valores JSON de estado y prioridad | `ToDo`/`InProgress`/`Done`, `Low`/`Medium`/`High` |
| Longitudes máximas | Pendiente de [database.md](../03-architecture/database.md) |
| Idioma de los mensajes de validación | Decidido: `es-ES` desde la API |
| Zona horaria de negocio | Decidido: `Europe/Madrid` |
| Ordenación predeterminada y desempate | §6 |
| Paginación | Decidido: `page`/`pageSize` (§6); tamaños por defecto y máximo propuestos |
| Concurrencia | Pendiente; §9 |
| 404 frente a 403 para recursos no visibles | 404 (requisitos §8, cuestión 11) |
| Rechazo de propiedades desconocidas | Rechazar con 400 |
| Publicación del OpenAPI | Generado desde la API y versionado en `docs/05-api/`, o escrito a mano antes de implementar |
