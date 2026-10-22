# TaskFlow — Requisitos funcionales

> **Identificador del documento:** TF-REQ-FR
>
> **Proyecto:** TaskFlow — Incorporación técnica al desarrollo de aplicaciones web completas
>
> **Estado:** base para la propuesta de arquitectura; revisado con el modelo de acceso por pertenencia a proyectos; implementación pendiente
>
> **Última revisión:** 2026-10-22 — roles `Admin`/`User`, visibilidad por pertenencia, borrado y restauración de proyectos por `Admin`
>
> **Prioridad:** MVP, salvo indicación expresa de opcionalidad
>
> **Responsables:** responsable técnico para requisitos; desarrolladores de backend y frontend para implementación

## 1. Propósito

Este documento especifica qué debe hacer TaskFlow. Los desarrolladores deberán convertir estos requisitos en una propuesta de arquitectura, modelo de datos, contratos de API, recorridos de usuario, pruebas e incidencias de GitHub. Las decisiones arquitectónicas requerirán revisión del responsable técnico antes de implementar las funcionalidades dependientes.

TaskFlow será una aplicación web ligera de gestión de proyectos y tareas para practicar autenticación, autorización por rol y por recurso, persistencia, colaboración, pruebas y despliegue en un entorno AWS aislado de producción.

## 2. Alcance y terminología

**Alcance del MVP:** autenticación, acceso por roles, visibilidad de proyectos por pertenencia, gestión de proyectos y tareas, asignación, filtros y ordenación, vista personal de tareas, gestión administrativa de proyectos archivados y panel de indicadores sencillo.

**Términos**

- **Proyecto:** agrupación de tareas relacionadas.
- **Tarea:** unidad de trabajo que pertenece exactamente a un proyecto.
- **Usuario asignado:** persona responsable de completar una tarea. Siempre es miembro del proyecto de la tarea.
- **Miembro de un proyecto:** usuario vinculado al proyecto mediante la relación de pertenencia (`AppUser_Project`). Se adquiere de forma automática al **crear** el proyecto o al **recibir la asignación** de una tarea del proyecto (véase FR-023).
- **Creador del proyecto:** usuario que creó el proyecto. Es miembro desde su creación.
- **Proyecto visible:** para `Admin`, cualquier proyecto (activo o archivado); para `User`, un proyecto **activo** del que es miembro.
- **`Admin`:** rol con acceso completo: consulta y gestiona todos los proyectos —incluidos los archivados—, sus tareas y asignaciones; es el único que puede archivar, restaurar y eliminar proyectos.
- **`User`:** rol que puede crear proyectos y gestionar por completo las tareas de los proyectos activos de los que es miembro; solo ve esos proyectos.
- **Archivado:** retirada no destructiva de las listas activas; los registros se conservan. Un proyecto archivado deja de ser visible para `User`.
- **Eliminación de proyecto:** retirada definitiva del proyecto, sus tareas y sus pertenencias. Solo `Admin` (FR-021).

Los nombres técnicos de roles serán `Admin` y `User`. Los estados de tarea serán «Pendiente» (`To Do`), «En curso» (`In Progress`) y «Completada» (`Done`); las prioridades serán «Baja» (`Low`), «Media» (`Medium`) y «Alta» (`High`). El contrato de API concretará su representación en los datos; las etiquetas de interfaz se mostrarán en español. Estos estados de tarea son independientes de los estados de trabajo del tablero de GitHub Projects.

**Fuera del MVP:** gestión de múltiples organizaciones, facturación, adjuntos, comentarios, notificaciones, interfaz de auditoría, actualizaciones en tiempo real, subtareas, roles personalizados, aplicación móvil, roles dentro de un proyecto (propietario, editor, lector…) y gestión manual de miembros (invitar o expulsar miembros sin asignar tareas).

## 3. Roles, visibilidad y acceso

### 3.1. Matriz de permisos

| Capacidad | `Admin` | `User` |
|---|:---:|:---:|
| Iniciar y cerrar sesión | Sí | Sí |
| Consultar el perfil propio | Sí | Sí |
| Consultar proyectos activos y sus tareas | Todos | Solo aquellos de los que es miembro |
| Consultar proyectos archivados y sus tareas | Sí | No |
| Crear proyectos | Sí | Sí (pasa a ser miembro) |
| Editar nombre y descripción de un proyecto | Cualquiera | Solo proyectos activos de los que es miembro |
| Archivar y restaurar proyectos | Sí | No |
| Eliminar proyectos | Sí | No |
| Crear, editar y eliminar tareas | En cualquier proyecto | En proyectos activos de los que es miembro |
| Asignar, reasignar o desasignar tareas | En cualquier proyecto | En proyectos activos de los que es miembro |
| Cambiar el estado de cualquier tarea | En cualquier proyecto | En proyectos activos de los que es miembro |
| Consultar «Mis tareas» | Sí | Sí |
| Consultar los indicadores del panel | Todos los proyectos activos | Solo sus proyectos visibles |

### 3.2. Reglas de visibilidad

- `User` ve un proyecto si y solo si **no está archivado** y **es miembro** de él (lo creó o tiene o ha tenido una tarea asignada en él; véase FR-023).
- Dentro de un proyecto visible, `User` ve **todas** sus tareas, estén asignadas a él, a otro miembro o sin asignar, y puede gestionarlas todas.
- `Admin` ve todos los proyectos. Las listas activas, «Mis tareas» y el panel excluyen los proyectos archivados también para `Admin`; los archivados se consultan en una vista administrativa específica (FR-022).

### 3.3. Aplicación de permisos y respuestas HTTP

Los permisos se aplicarán en el backend. Ocultar controles en la interfaz no impedirá por sí solo que un usuario invoque directamente la API. Las identidades sin un rol admitido no tendrán acceso a las operaciones de negocio.

| Situación | Respuesta de la API |
|---|---|
| Sin token o con token inválido o caducado | HTTP 401 |
| Token válido sin rol admitido | HTTP 403 |
| `User` accede a un proyecto o tarea que no le es visible (no es miembro, o el proyecto está archivado) | HTTP 404, sin revelar si el recurso existe |
| `User` solicita una operación reservada a `Admin` sobre un proyecto visible (archivar, restaurar, eliminar) | HTTP 403 |
| El recurso no existe | HTTP 404 |

Responder 404 ante recursos no visibles evita que un `User` descubra proyectos ajenos probando identificadores. Esta convención es una propuesta que se confirmará en fase 0 (§8, cuestión 11).

## 4. Requisitos funcionales

Los identificadores no se renumeran. Los requisitos FR-021 a FR-023 se añadieron en la revisión de 2026-10-22 y se ubican en el bloque temático correspondiente.

### 4.1. Autenticación e identidad

#### FR-001 — Iniciar sesión

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero iniciar sesión de forma segura para acceder a TaskFlow.

**Criterios de aceptación**

- Una visita sin autenticar a una pantalla protegida dirige al proveedor de identidad configurado.
- Tras una autenticación correcta, el usuario vuelve a la aplicación.
- Las operaciones protegidas de la API rechazan las solicitudes sin un token de acceso válido con HTTP 401.
- TaskFlow no gestiona ni almacena contraseñas; esa responsabilidad corresponde al proveedor de identidad.
- La interfaz ofrece una página de registro que redirige al registro del proveedor de identidad; los usuarios registrados obtienen el rol `User` y nunca `Admin`.

#### FR-002 — Cerrar sesión y gestionar su caducidad

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero cerrar sesión y saber cuándo ha caducado.

**Criterios de aceptación**

- Una acción visible finaliza la sesión de la aplicación e invoca el cierre de sesión del proveedor de identidad.
- La interfaz elimina los tokens y datos protegidos de su estado y caché al cerrar sesión.
- La API rechaza los tokens caducados o inválidos.
- El frontend gestiona la caducidad sin seguir mostrando datos protegidos de la sesión anterior.
- La documentación de autenticación explica la vigencia residual que puede tener un JWT ya emitido tras el cierre de sesión; si se exige revocación inmediata, se acordará un mecanismo adicional en fase 0.

#### FR-003 — Perfil y rol del usuario actual

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero consultar mi identidad y disponer de los permisos correspondientes.

**Criterios de aceptación**

- El usuario autenticado puede ver su nombre visible y, cuando esté disponible, su correo.
- Los permisos de `Admin` y `User` proceden de identidad validada o de un mapeo aprobado en el servidor.
- La ausencia de roles admitidos o la presencia exclusiva de roles desconocidos deniega las operaciones de negocio con HTTP 403 para un token válido.
- Cada identidad autenticada se corresponde con un único registro local `AppUser` identificado por el `sub` del token; el correo o el nombre no sirven como identificador.
- El equipo documenta cómo se crean los usuarios y roles de la demostración.

### 4.2. Gestión de proyectos

#### FR-004 — Listar proyectos visibles

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero consultar los proyectos en los que participo para localizar mi trabajo.

**Criterios de aceptación**

- `Admin` obtiene todos los proyectos activos.
- `User` obtiene únicamente los proyectos activos de los que es miembro: los que creó y aquellos en los que tiene o ha tenido una tarea asignada.
- Un proyecto en el que `User` no es miembro no aparece en su lista, aunque exista y esté activo.
- La lista incluye, al menos, nombre y descripción.
- Si no hay proyectos visibles, se muestra un estado vacío informativo que invita a crear uno.
- Los proyectos archivados se excluyen de la lista activa para ambos roles.

#### FR-005 — Consultar un proyecto

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero abrir un proyecto para consultar sus datos y tareas.

**Criterios de aceptación**

- Quien tenga el proyecto visible puede consultar su nombre, descripción, miembros y todas sus tareas.
- Un proyecto inexistente devuelve HTTP 404 en la API y un mensaje claro en la interfaz.
- Si `User` accede por identificador a un proyecto del que no es miembro o que está archivado, la API responde HTTP 404 y la interfaz muestra el mismo mensaje que para un recurso inexistente.
- `Admin` puede abrir un proyecto archivado desde la vista de archivados (FR-022); la interfaz indica claramente que está archivado.

#### FR-006 — Crear un proyecto

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero crear proyectos para organizar el trabajo.

**Criterios de aceptación**

- `Admin` y `User` pueden crear un proyecto con nombre obligatorio y descripción opcional.
- Los nombres vacíos o compuestos solo por espacios generan un error de validación.
- Al crear el proyecto, el servidor registra a su autor como creador y como miembro en la misma transacción; el creador no se acepta como dato del cliente.
- El proyecto se crea activo, aparece de inmediato en la lista del creador y sigue visible tras recargar la página.

#### FR-007 — Editar un proyecto

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero modificar los datos de un proyecto en el que participo.

**Criterios de aceptación**

- `Admin` puede actualizar nombre y descripción de cualquier proyecto, activo o archivado.
- `User` puede actualizar nombre y descripción de los proyectos activos de los que es miembro.
- Los nombres inválidos se rechazan con las mismas reglas que en la creación.
- La edición no permite cambiar el creador, el estado de archivado ni los miembros.
- Los cambios se conservan tras reiniciar la aplicación.
- Si `User` intenta editar un proyecto que no le es visible, la API responde HTTP 404.

#### FR-008 — Archivar un proyecto

**Prioridad:** obligatoria

**Historia de usuario:** como `Admin`, quiero retirar un proyecto terminado de las listas activas sin perder sus datos.

**Criterios de aceptación**

- `Admin` puede archivar un proyecto activo después de confirmar la acción.
- El proyecto y sus tareas desaparecen de las listas activas, de «Mis tareas» y del panel de todos los usuarios.
- Los miembros con rol `User` dejan de ver el proyecto y sus tareas; un acceso directo responde HTTP 404.
- El archivado no elimina tareas ni pertenencias, y se registra el instante del archivado.
- Si `User` intenta archivar un proyecto visible, la API responde HTTP 403.

#### FR-021 — Eliminar un proyecto

**Prioridad:** obligatoria

**Historia de usuario:** como `Admin`, quiero eliminar un proyecto creado por error o que no debe conservarse.

**Criterios de aceptación**

- `Admin` puede eliminar un proyecto, activo o archivado, después de una confirmación explícita que advierte que también se eliminan sus tareas.
- Al eliminar un proyecto se eliminan con él sus tareas y sus pertenencias, sin dejar tareas huérfanas.
- El proyecto eliminado deja de aparecer en cualquier consulta, en «Mis tareas» y en el panel, y su identificador devuelve HTTP 404.
- Si `User` intenta eliminar un proyecto visible, la API responde HTTP 403.
- La interfaz recomienda archivar cuando el objetivo sea conservar el historial.
- La propuesta de arquitectura documenta si el borrado será físico o lógico (§8, cuestión 4).

#### FR-022 — Consultar y restaurar proyectos archivados

**Prioridad:** obligatoria

**Historia de usuario:** como `Admin`, quiero consultar los proyectos archivados y reactivarlos si vuelven a ser necesarios.

**Criterios de aceptación**

- `Admin` dispone de una vista de proyectos archivados, separada de la lista activa, con nombre, descripción y fecha de archivado.
- `Admin` puede abrir un proyecto archivado y consultar, crear, editar, asignar, cambiar de estado y eliminar sus tareas.
- `Admin` puede restaurar un proyecto archivado tras confirmarlo; vuelve a las listas activas y al panel, y sus miembros vuelven a verlo.
- `User` no puede acceder a la vista ni a la operación de restauración: la vista no se muestra y la API responde HTTP 403.

#### FR-023 — Pertenencia a proyectos

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero ver automáticamente los proyectos que he creado o en los que me han asignado trabajo, sin gestionar miembros manualmente.

**Criterios de aceptación**

- Al crear un proyecto, su creador queda registrado como miembro.
- Al asignar una tarea a un usuario que aún no es miembro del proyecto, el servidor lo registra como miembro en la misma transacción que la asignación.
- Cada pareja usuario-proyecto figura como mucho una vez; asignar varias tareas al mismo usuario no duplica la pertenencia.
- Desasignar o reasignar una tarea no elimina la pertenencia del usuario anterior en el MVP; la retirada de pertenencias queda pendiente (§8, cuestión 10).
- La pertenencia se elimina únicamente al eliminar el proyecto (FR-021).
- La interfaz del proyecto muestra la lista de miembros.

### 4.3. Gestión de tareas

#### FR-009 — Crear una tarea

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero crear tareas dentro de un proyecto en el que participo.

**Criterios de aceptación**

- Cada tarea pertenece exactamente a un proyecto existente.
- `User` solo puede crear tareas en proyectos activos de los que es miembro; `Admin`, en cualquier proyecto, incluidos los archivados.
- Son obligatorios el título, proyecto, estado y prioridad; descripción, usuario asignado y fecha de vencimiento son opcionales.
- Si no se indica estado, la propuesta de contrato definirá si se toma «Pendiente» (`To Do`) por defecto.
- Se rechazan títulos vacíos, valores no admitidos y referencias a proyectos inválidos.
- Si se indica usuario asignado, se aplican las reglas de FR-013 y FR-023.
- La tarea se guarda y aparece en el proyecto correspondiente.
- Si `User` intenta crear una tarea en un proyecto que no le es visible, la API responde HTTP 404.

#### FR-010 — Listar y consultar tareas

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero consultar las tareas y sus datos actuales.

**Criterios de aceptación**

- `User` puede consultar todas las tareas de sus proyectos visibles, independientemente de a quién estén asignadas.
- `Admin` puede consultar las tareas de cualquier proyecto; las listas generales solo incluyen proyectos activos.
- El detalle incluye título, descripción, proyecto, estado, prioridad, usuario asignado si existe, vencimiento si existe y fechas y horas de creación y última modificación.
- Un identificador de tarea inexistente, o perteneciente a un proyecto no visible para `User`, devuelve HTTP 404 y un estado de recurso no encontrado en la interfaz.

#### FR-011 — Editar una tarea

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero corregir los datos de una tarea de mis proyectos.

**Criterios de aceptación**

- Quien pueda gestionar las tareas del proyecto (`Admin`, o `User` miembro de un proyecto activo) puede modificar título, descripción, prioridad y vencimiento.
- Una tarea no se puede mover a otro proyecto en el MVP.
- Los valores inválidos se rechazan sin dejar cambios parciales.
- La asignación y el cambio de estado se rigen por FR-013 y FR-014.

#### FR-012 — Eliminar una tarea

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero eliminar una tarea innecesaria de mis proyectos.

**Criterios de aceptación**

- `Admin`, o `User` miembro de un proyecto activo, puede eliminar cualquier tarea del proyecto tras confirmar la acción.
- Las tareas eliminadas dejan de aparecer en las consultas normales, en «Mis tareas» y en los indicadores del panel.
- Eliminar una tarea no elimina la pertenencia de su usuario asignado.
- La propuesta de arquitectura documenta si el borrado será físico o lógico.

#### FR-013 — Asignar una tarea

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero asignar tareas a una persona o dejarlas sin asignar.

**Criterios de aceptación**

- Quien pueda gestionar las tareas del proyecto puede elegir un usuario válido de la lista de usuarios asignables (usuarios de demostración cargados o fuente equivalente acordada).
- El usuario elegido puede ser cualquier usuario asignable, sea o no miembro del proyecto; si no lo era, pasa a serlo (FR-023) y el proyecto aparece en su lista.
- Se puede cambiar o retirar la asignación.
- Se rechazan asignaciones a usuarios desconocidos.
- Una tarea solo puede quedar asignada a un miembro del proyecto; la base de datos garantiza esta regla.
- La identidad asignada se conserva tras recargar.

#### FR-014 — Actualizar el estado de una tarea

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero actualizar el progreso de las tareas de mis proyectos.

**Criterios de aceptación**

- Los estados admitidos son «Pendiente» (`To Do`), «En curso» (`In Progress`) y «Completada» (`Done`).
- Se permite cualquier transición entre los tres estados.
- `Admin` puede cambiar el estado de cualquier tarea.
- `User` puede cambiar el estado de cualquier tarea de un proyecto activo del que es miembro, esté asignada a él, a otro miembro o sin asignar.
- Si `User` intenta cambiar el estado de una tarea de un proyecto que no le es visible, la API responde HTTP 404.
- Los cambios se guardan y se reflejan en las listas y en el panel.

#### FR-015 — Filtrar y ordenar tareas

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero acotar y ordenar las listas para encontrar tareas.

**Criterios de aceptación**

- Se puede filtrar por proyecto, estado, prioridad y usuario asignado (incluida la opción «sin asignar»).
- Los filtros solo ofrecen y devuelven datos de proyectos visibles para el usuario; filtrar por un proyecto no visible devuelve un resultado vacío o HTTP 404, según defina el contrato, sin revelar datos.
- Los filtros se pueden limpiar para recuperar la lista sin filtros.
- Se puede ordenar por fecha de vencimiento o de creación.
- La interfaz muestra estados de carga y vacío claros.

#### FR-016 — Consultar mis tareas

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero una vista de las tareas que tengo asignadas.

**Criterios de aceptación**

- Un usuario con rol admitido puede abrir la vista «Mis tareas».
- La vista muestra únicamente las tareas asignadas a él en proyectos activos, derivando la identidad del usuario autenticado.
- Si no tiene tareas, se muestra un estado vacío informativo.
- Alterar un parámetro de identidad en la solicitud no permite consultar ni modificar las tareas de otra persona.

### 4.4. Panel de indicadores

#### FR-017 — Indicadores generales

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero conocer el estado general de los proyectos y tareas que me afectan.

**Criterios de aceptación**

- El panel muestra el número de proyectos activos y de tareas abiertas, completadas y vencidas.
- Para `Admin`, los recuentos abarcan todos los proyectos activos; para `User`, solo sus proyectos visibles.
- Una tarea abierta está «Pendiente» o «En curso»; una tarea completada está «Completada».
- Una tarea está vencida cuando su fecha de vencimiento es anterior a la fecha actual y no está completada. Una tarea sin vencimiento no está vencida.
- La fecha actual se determina en la zona horaria de negocio `Europe/Madrid`, de forma coherente entre API e interfaz.
- Los indicadores respetan las mismas reglas de visibilidad que las listas y excluyen proyectos archivados o eliminados y sus tareas, así como tareas eliminadas.
- Los valores se obtienen de datos persistidos y se actualizan tras los cambios de proyectos y tareas.

### 4.5. Respuesta de la aplicación y fiabilidad

#### FR-018 — Mensajes y estados de las operaciones

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero recibir mensajes claros sobre mis acciones y los errores.

**Criterios de aceptación**

- Los campos obligatorios muestran errores de validación que ayudan a corregirlos.
- Las operaciones de creación, edición, archivado, restauración y borrado muestran éxito o error.
- Las solicitudes pendientes muestran un estado de carga y evitan envíos duplicados accidentales.
- La interfaz distingue errores de autenticación, permisos, recurso no encontrado y servidor, sin exponer detalles técnicos sensibles.

#### FR-019 — Persistencia tras reinicios

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, espero que los datos guardados se conserven al reiniciar la aplicación.

**Criterios de aceptación**

- Proyectos, tareas, asignaciones y pertenencias se conservan al reiniciar frontend y backend.
- La inicialización de base de datos y las migraciones están documentadas y son reproducibles.
- Reiniciar los contenedores no borra los volúmenes persistentes en el entorno documentado. Los comandos de eliminación de datos se identifican por separado.

#### FR-020 — Acceso integrado al entorno desplegado

**Prioridad:** obligatoria

**Historia de usuario:** como evaluador, quiero utilizar TaskFlow en su entorno de demostración de AWS.

**Criterios de aceptación**

- El responsable técnico puede acceder al frontend mediante una URL HTTPS aprobada.
- Se puede completar este recorrido con usuarios de demostración: crear proyecto y tarea como `User`, asignarla a otro `User`, iniciar sesión como ese segundo usuario, ver el proyecto, cambiar el estado y comprobar el panel; después, como `Admin`, archivar el proyecto y comprobar que ambos `User` dejan de verlo.
- API, base de datos e identidad desplegadas funcionan sin depender de la máquina local del desarrollador.
- Una guía operativa explica cómo reproducir el despliegue y revertir una entrega.
- Los recursos y el gasto de AWS se aprueban antes del aprovisionamiento.

## 5. Reglas de negocio

| Regla | Descripción |
|---|---|
| BR-001 | Cada tarea pertenece a un proyecto existente y no puede cambiar de proyecto. |
| BR-002 | Los nombres de proyecto y títulos de tarea no pueden estar vacíos ni contener solo espacios. |
| BR-003 | La prioridad será Baja, Media o Alta. |
| BR-004 | El estado será Pendiente, En curso o Completada. |
| BR-005 | Solo `Admin` puede archivar, restaurar y eliminar proyectos, y consultar proyectos archivados. |
| BR-006 | `User` puede crear proyectos y editar los proyectos activos de los que es miembro. |
| BR-007 | `User` puede crear, editar, eliminar, asignar y cambiar el estado de cualquier tarea de un proyecto activo del que es miembro, incluidas las tareas sin asignar o asignadas a otros. |
| BR-008 | Los proyectos archivados y sus tareas se excluyen de las vistas activas, de «Mis tareas» y de los indicadores, y no son visibles para `User`. |
| BR-009 | Las marcas temporales representan instantes de manera coherente; la visualización tiene una estrategia de zona horaria definida. Si el vencimiento se modela como fecha de calendario, no se convierte en un instante y se compara con la fecha de negocio acordada. |
| BR-010 | Una identidad sin un rol admitido no puede ejecutar operaciones de negocio. |
| BR-011 | `User` solo ve los proyectos activos de los que es miembro. La pertenencia se adquiere al crear el proyecto o al recibir la asignación de una de sus tareas. |
| BR-012 | Una tarea solo puede asignarse a un miembro de su proyecto; asignarla a un usuario asignable que no es miembro lo convierte en miembro en la misma operación. |
| BR-013 | Un usuario figura como mucho una vez como miembro de un proyecto. |
| BR-014 | Eliminar un proyecto elimina sus tareas y pertenencias; archivarlo las conserva. |
| BR-015 | Los recursos no visibles para `User` responden como inexistentes (HTTP 404). |

## 6. Recorridos para la demostración de aceptación

Usuarios de demostración previstos: «Admin», «Usuario A», «Usuario B» y «Usuario C», los tres últimos con rol `User`.

### Recorrido A — `User` crea y asigna trabajo

1. Iniciar sesión como «Usuario A».
2. Crear el proyecto «Rediseño del sitio web» y comprobar que aparece en su lista.
3. Crear la tarea «Construir la página de inicio», prioridad Alta y estado Pendiente.
4. Asignarla a «Usuario B» y establecer una fecha de vencimiento futura.
5. Comprobar que «Usuario B» figura como miembro del proyecto.
6. Recargar y comprobar que proyecto, tarea, asignación y pertenencia se conservan.

### Recorrido B — `User` asignado colabora en el proyecto

1. Iniciar sesión como «Usuario B».
2. Comprobar que «Rediseño del sitio web» aparece en su lista de proyectos.
3. Abrir «Mis tareas» y localizar «Construir la página de inicio».
4. Cambiar el estado a En curso y después a Completada.
5. Crear una tarea nueva sin asignar en el proyecto y editar su prioridad.
6. Recargar y comprobar el cambio en la lista y en los indicadores.
7. Intentar archivar o eliminar el proyecto y comprobar que la API responde HTTP 403.

### Recorrido C — Visibilidad, validación y registros inexistentes

1. Iniciar sesión como «Usuario C» y comprobar que «Rediseño del sitio web» no aparece en su lista ni en su panel.
2. Abrir por URL el proyecto y una de sus tareas, y comprobar HTTP 404 y el mensaje de recurso no encontrado.
3. Intentar crear un proyecto con nombre vacío y comprobar la validación.
4. Abrir la URL de un proyecto inexistente y comprobar el mensaje correspondiente.
5. Intentar crear una tarea para un proyecto inexistente y comprobar que la API la rechaza.

### Recorrido D — Validación en AWS

1. Abrir la aplicación desplegada mediante HTTPS.
2. Autenticarse con los usuarios de demostración configurados.
3. Completar los recorridos A, B, C y E con los servicios desplegados.
4. Mostrar las comprobaciones de salud, los registros, la documentación y el flujo de CI utilizado.

### Recorrido E — `Admin` supervisa, archiva, restaura y elimina

1. Iniciar sesión como «Admin» y comprobar que ve todos los proyectos activos, incluidos los que no creó, y los recuentos globales del panel.
2. Archivar «Rediseño del sitio web» tras confirmar.
3. Iniciar sesión como «Usuario A» y «Usuario B» y comprobar que el proyecto ya no aparece y que su URL responde HTTP 404.
4. Como «Admin», abrir la vista de archivados, consultar el proyecto y restaurarlo; comprobar que sus miembros vuelven a verlo.
5. Crear un proyecto de prueba, eliminarlo tras confirmar y comprobar que sus tareas desaparecen de todas las vistas y del panel.

## 7. Trazabilidad de requisitos y entrega

Cada requisito se vinculará a su implementación y verificación.

| Campo | Evidencia requerida |
|---|---|
| Requisito | Identificador `FR-###` en la incidencia |
| Responsabilidad | Backend, frontend o compartida |
| Implementación | PR vinculadas |
| Verificación | Nombre de prueba, lista de comprobación o prueba manual documentada |
| Estado | Pendiente / Preparado / En curso / En revisión / Finalizado |
| Entrega | Hito o versión desplegada acordada |

**Criterio de finalización de un requisito:** permisos y visibilidad aplicados en el backend cuando corresponda, interacción de frontend utilizable, contrato acordado, validación y errores contemplados, verificación documentada y PR aprobadas. Una pantalla con datos simulados no completa un requisito integrado.

## 8. Cuestiones de diseño para la fase 0

Las respuestas deberán proponerse en la arquitectura y revisarse antes de implementar las funcionalidades dependientes. La [visión general del sistema](../03-architecture/system-overview.md) recoge las propuestas iniciales y lo que sigue pendiente.

1. ¿Cómo se mapearán los datos de identidad y roles de Keycloak (`Admin`, `User`) a los permisos de la aplicación?
2. ¿Cómo se obtendrá la lista de usuarios asignables, ahora accesible también para `User`, sin exponer credenciales administrativas de Keycloak al navegador ni más datos personales de los necesarios?
3. ¿Cómo se normalizarán errores y mensajes de validación de la API?
4. ¿El borrado de tareas y de proyectos será físico o lógico y por qué? (Propuesta: físico con cascada, [ADR-0002](../03-architecture/adr/0002-borrado-fisico.md).)
5. ¿Cómo se publicará un contrato de API que permita trabajar en paralelo?
6. ¿Qué diseño AWS proporcionará HTTPS, secretos, persistencia e identidad con un coste aceptable?
7. ¿Qué pruebas automatizadas y verificaciones básicas demostrarán cada recorrido principal, incluidas las denegaciones por visibilidad?
8. ¿Qué tipo tendrá el vencimiento? (Zona horaria de negocio decidida: `Europe/Madrid`.)
9. ¿Qué vigencia tendrán los tokens y qué garantías de cierre de sesión se requieren para la demostración?
10. ¿Debe retirarse la pertenencia de un usuario cuando deja de tener tareas asignadas en un proyecto que no creó? (En el MVP se conserva.)
11. ¿Se confirma HTTP 404 para recursos existentes pero no visibles para `User`, frente a HTTP 403?
12. ¿Cómo se garantiza que dos asignaciones simultáneas del mismo usuario al mismo proyecto no dupliquen ni hagan fallar la pertenencia?

## 9. Control de cambios y aprobación

- El responsable técnico controla los cambios de requisitos y prioridades.
- Los desarrolladores pueden proponer ajustes mediante incidencias con su impacto en API, interfaz, datos, pruebas y calendario.
- Los cambios aceptados actualizan este documento y sus criterios de aceptación en la misma PR o en una PR de documentación vinculada.
- Todos los requisitos marcados como obligatorios forman parte del MVP. Las mejoras opcionales no retrasarán la funcionalidad requerida ni la entrega en AWS.

**Registro de cambios**

| Fecha | Cambio |
|---|---|
| 2026-10-22 | Registro de usuarios mediante Keycloak (FR-001); zona horaria de negocio `Europe/Madrid` (FR-017). |
| 2026-10-22 | El rol `Member` pasa a llamarse `User` y obtiene gestión completa de proyectos y tareas en los proyectos de los que es miembro. Se introduce la pertenencia a proyectos (FR-023, BR-011 a BR-015) como regla de visibilidad. `Admin` puede consultar y restaurar archivados (FR-022) y eliminar proyectos (FR-021). Se añade el recorrido E. |

**Punto de aprobación:** la fase 0 finalizará cuando el responsable técnico acepte la propuesta de arquitectura, contratos iniciales, modelo de datos y plan de implementación frente a estos requisitos.
