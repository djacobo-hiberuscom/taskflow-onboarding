# TaskFlow — Requisitos funcionales

> **Identificador del documento:** TF-REQ-FR
>
> **Proyecto:** TaskFlow — Incorporación técnica al desarrollo de aplicaciones web completas
>
> **Estado:** base para la propuesta de arquitectura; implementación pendiente
>
> **Prioridad:** MVP, salvo indicación expresa de opcionalidad
>
> **Responsables:** responsable técnico para requisitos; desarrolladores de backend y frontend para implementación

## 1. Propósito

Este documento especifica qué debe hacer TaskFlow. Los desarrolladores deberán convertir estos requisitos en una propuesta de arquitectura, modelo de datos, contratos de API, recorridos de usuario, pruebas e incidencias de GitHub. Las decisiones arquitectónicas requerirán revisión del responsable técnico antes de implementar las funcionalidades dependientes.

TaskFlow será una aplicación web ligera de gestión de proyectos y tareas para practicar autenticación, autorización, persistencia, colaboración, pruebas y despliegue en un entorno AWS aislado de producción.

## 2. Alcance y terminología

**Alcance del MVP:** autenticación, acceso por roles, gestión de proyectos y tareas, asignación, filtros y ordenación, vista personal de tareas y panel de indicadores sencillo.

**Términos**

- **Proyecto:** agrupación de tareas relacionadas.
- **Tarea:** unidad de trabajo que pertenece exactamente a un proyecto.
- **Usuario asignado:** persona responsable de completar una tarea.
- **`Admin`:** rol autorizado para gestionar proyectos, tareas y asignaciones.
- **`Member`:** rol autorizado para consultar proyectos activos y modificar el estado de sus tareas asignadas.
- **Archivado:** retirada no destructiva de las listas activas; los registros se conservan. La restauración es opcional.

Los nombres técnicos de roles se conservarán como `Admin` y `Member`. Los estados de tarea serán «Pendiente» (`To Do`), «En curso» (`In Progress`) y «Completada» (`Done`); las prioridades serán «Baja» (`Low`), «Media» (`Medium`) y «Alta» (`High`). El contrato de API concretará su representación en los datos; las etiquetas de interfaz se mostrarán en español. Estos estados de tarea son independientes de los estados de trabajo del tablero de GitHub Projects.

**Fuera del MVP:** gestión de múltiples organizaciones, facturación, adjuntos, comentarios, notificaciones, interfaz de auditoría, actualizaciones en tiempo real, subtareas, roles personalizados, aplicación móvil y gestión avanzada de miembros por proyecto.

## 3. Roles y acceso

| Capacidad | `Admin` | `Member` |
|---|:---:|:---:|
| Iniciar y cerrar sesión | Sí | Sí |
| Consultar el perfil propio | Sí | Sí |
| Consultar todos los proyectos activos y sus tareas | Sí | Sí |
| Crear, editar y archivar proyectos | Sí | No |
| Crear, editar y eliminar tareas | Sí | No |
| Asignar o reasignar tareas | Sí | No |
| Cambiar el estado de tareas propias | Sí | Sí |
| Cambiar el estado de tareas de otros usuarios o sin asignar | Sí | No |
| Consultar las tareas propias | Sí | Sí |
| Consultar los indicadores del panel | Sí | Sí |

Los permisos se aplicarán en el backend. Ocultar controles en la interfaz no impedirá por sí solo que un usuario invoque directamente la API. Las identidades sin un rol admitido no tendrán acceso a las operaciones de negocio.

El MVP no contempla restricciones de lectura por pertenencia a un proyecto: ambos roles podrán consultar todos los proyectos activos. La vista «Mis tareas» será un filtro personal, no un límite adicional de lectura.

## 4. Requisitos funcionales

### 4.1. Autenticación e identidad

#### FR-001 — Iniciar sesión

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero iniciar sesión de forma segura para acceder a TaskFlow.

**Criterios de aceptación**

- Una visita sin autenticar a una pantalla protegida dirige al proveedor de identidad configurado.
- Tras una autenticación correcta, el usuario vuelve a la aplicación.
- Las operaciones protegidas de la API rechazan las solicitudes sin un token de acceso válido con HTTP 401.
- TaskFlow no gestiona ni almacena contraseñas; esa responsabilidad corresponde al proveedor de identidad.

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
- Los permisos de `Admin` y `Member` proceden de identidad validada o de un mapeo aprobado en el servidor.
- La ausencia de roles admitidos o la presencia exclusiva de roles desconocidos deniega las operaciones de negocio con HTTP 403 para un token válido.
- El equipo documenta cómo se crean los usuarios y roles de la demostración.

### 4.2. Gestión de proyectos

#### FR-004 — Listar proyectos activos

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero consultar los proyectos para localizar su trabajo relacionado.

**Criterios de aceptación**

- Los usuarios autenticados con un rol admitido pueden consultar todos los proyectos activos.
- La lista incluye, al menos, nombre y descripción.
- Si no hay proyectos, se muestra un estado vacío informativo.
- Los proyectos archivados se excluyen de la lista activa predeterminada.

#### FR-005 — Consultar un proyecto

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero abrir un proyecto para consultar sus datos y tareas.

**Criterios de aceptación**

- Los usuarios con permiso de lectura pueden consultar nombre, descripción y tareas asociadas a un proyecto activo.
- Un proyecto inexistente devuelve HTTP 404 en la API y un mensaje claro en la interfaz.
- El acceso a proyectos archivados se concretará conforme a FR-008; no aparecerán como proyectos activos al acceder directamente por su identificador.

#### FR-006 — Crear un proyecto

**Prioridad:** obligatoria

**Historia de usuario:** como `Admin`, quiero crear proyectos para organizar el trabajo.

**Criterios de aceptación**

- `Admin` puede crear un proyecto con nombre obligatorio y descripción opcional.
- Los nombres vacíos o compuestos solo por espacios generan un error de validación.
- Un proyecto válido se guarda y sigue visible tras recargar la página.
- Una solicitud de `Member` se rechaza con HTTP 403.

#### FR-007 — Editar un proyecto

**Prioridad:** obligatoria

**Historia de usuario:** como `Admin`, quiero modificar los datos de un proyecto.

**Criterios de aceptación**

- `Admin` puede actualizar nombre y descripción.
- Los nombres inválidos se rechazan con las mismas reglas que en la creación.
- Los cambios se conservan tras reiniciar la aplicación.
- `Member` no puede editar proyectos.

#### FR-008 — Archivar un proyecto

**Prioridad:** obligatoria

**Historia de usuario:** como `Admin`, quiero retirar un proyecto terminado de las listas activas sin perder sus datos.

**Criterios de aceptación**

- `Admin` puede archivar un proyecto activo después de confirmar la acción.
- El proyecto desaparece de la lista activa predeterminada.
- El archivado no elimina físicamente sus tareas.
- `Member` no puede archivar proyectos.
- El equipo documenta si existirá una vista administrativa para consultar los proyectos archivados y qué consultas o cambios se permiten sobre ellos y sus tareas.
- Restaurar proyectos es opcional y no es necesario para completar el MVP.

### 4.3. Gestión de tareas

#### FR-009 — Crear una tarea

**Prioridad:** obligatoria

**Historia de usuario:** como `Admin`, quiero crear tareas dentro de un proyecto.

**Criterios de aceptación**

- Cada tarea pertenece exactamente a un proyecto existente y activo.
- Son obligatorios el título, proyecto, estado y prioridad; descripción, usuario asignado y fecha de vencimiento son opcionales.
- Se rechazan títulos vacíos, valores no admitidos y referencias a proyectos inválidos.
- La tarea se guarda y aparece en el proyecto correspondiente.
- `Member` no puede crear tareas.

#### FR-010 — Listar y consultar tareas

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero consultar las tareas y sus datos actuales.

**Criterios de aceptación**

- Los usuarios con rol admitido pueden consultar las tareas de los proyectos activos.
- El detalle incluye título, descripción, proyecto, estado, prioridad, usuario asignado si existe, vencimiento si existe y fecha y hora de creación.
- Un identificador de tarea inexistente devuelve HTTP 404 y un estado de recurso no encontrado en la interfaz.

#### FR-011 — Editar una tarea

**Prioridad:** obligatoria

**Historia de usuario:** como `Admin`, quiero corregir los datos de una tarea.

**Criterios de aceptación**

- `Admin` puede modificar título, descripción, prioridad y vencimiento.
- Los valores inválidos se rechazan sin dejar cambios parciales.
- `Member` no puede modificar esos campos.
- La asignación y el cambio de estado se rigen por FR-013 y FR-014.

#### FR-012 — Eliminar una tarea

**Prioridad:** obligatoria

**Historia de usuario:** como `Admin`, quiero eliminar una tarea innecesaria.

**Criterios de aceptación**

- `Admin` puede eliminar una tarea tras confirmar la acción.
- Las tareas eliminadas dejan de aparecer en las consultas normales y en los indicadores del panel.
- `Member` no puede eliminar tareas.
- La propuesta de arquitectura documenta si el borrado será físico o lógico.

#### FR-013 — Asignar una tarea

**Prioridad:** obligatoria

**Historia de usuario:** como `Admin`, quiero asignar tareas a usuarios o dejarlas sin asignar.

**Criterios de aceptación**

- `Admin` puede elegir un usuario válido de una lista de usuarios de demostración o de una fuente equivalente acordada.
- `Admin` puede cambiar o retirar la asignación.
- Se rechazan asignaciones a usuarios desconocidos.
- La identidad asignada se conserva tras recargar.
- `Member` no puede asignar ni reasignar tareas.

#### FR-014 — Actualizar el estado de una tarea

**Prioridad:** obligatoria

**Historia de usuario:** como usuario asignado, quiero actualizar el progreso de mi trabajo.

**Criterios de aceptación**

- Los estados admitidos son «Pendiente» (`To Do`), «En curso» (`In Progress`) y «Completada» (`Done`).
- `Admin` puede cambiar el estado de cualquier tarea de un proyecto activo.
- `Member` solo puede cambiar el estado de una tarea asignada a su identidad autenticada.
- Si `Member` intenta cambiar una tarea sin asignar o asignada a otra persona, la API responde HTTP 403.
- Los cambios se guardan y se reflejan en las listas y en el panel.

#### FR-015 — Filtrar y ordenar tareas

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero acotar y ordenar las listas para encontrar tareas.

**Criterios de aceptación**

- Se puede filtrar por proyecto, estado y prioridad.
- Los filtros se pueden limpiar para recuperar la lista sin filtros.
- Se puede ordenar por fecha de vencimiento o de creación.
- La interfaz muestra estados de carga y vacío claros.

#### FR-016 — Consultar mis tareas

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero una vista de las tareas que tengo asignadas.

**Criterios de aceptación**

- Un usuario con rol admitido puede abrir la vista «Mis tareas».
- La vista muestra únicamente sus tareas de proyectos activos, derivando la identidad del usuario autenticado.
- Si no tiene tareas, se muestra un estado vacío informativo.
- Alterar un parámetro de identidad no permite modificar tareas ajenas. La consulta general de tareas de otros usuarios sigue permitida según FR-010.

### 4.4. Panel de indicadores

#### FR-017 — Indicadores generales

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero conocer el estado general de los proyectos y tareas.

**Criterios de aceptación**

- El panel muestra proyectos activos y tareas abiertas, completadas y vencidas.
- Una tarea abierta está «Pendiente» o «En curso»; una tarea completada está «Completada».
- Una tarea está vencida cuando su fecha de vencimiento es anterior a la fecha actual y no está completada. Una tarea sin vencimiento no está vencida.
- El equipo define una zona horaria de negocio para determinar la fecha actual de forma coherente entre API e interfaz.
- Los indicadores respetan las mismas reglas de lectura que las listas y excluyen proyectos archivados y sus tareas, así como tareas eliminadas.
- Los valores se obtienen de datos persistidos y se actualizan tras los cambios de tareas.

### 4.5. Respuesta de la aplicación y fiabilidad

#### FR-018 — Mensajes y estados de las operaciones

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, quiero recibir mensajes claros sobre mis acciones y los errores.

**Criterios de aceptación**

- Los campos obligatorios muestran errores de validación que ayudan a corregirlos.
- Las operaciones de creación, edición, archivado y borrado muestran éxito o error.
- Las solicitudes pendientes muestran un estado de carga y evitan envíos duplicados accidentales.
- La interfaz distingue errores de autenticación, permisos, recurso no encontrado y servidor, sin exponer detalles técnicos sensibles.

#### FR-019 — Persistencia tras reinicios

**Prioridad:** obligatoria

**Historia de usuario:** como usuario, espero que los datos guardados se conserven al reiniciar la aplicación.

**Criterios de aceptación**

- Proyectos, tareas y asignaciones se conservan al reiniciar frontend y backend.
- La inicialización de base de datos y las migraciones están documentadas y son reproducibles.
- Reiniciar los contenedores no borra los volúmenes persistentes en el entorno documentado. Los comandos de eliminación de datos se identifican por separado.

#### FR-020 — Acceso integrado al entorno desplegado

**Prioridad:** obligatoria

**Historia de usuario:** como evaluador, quiero utilizar TaskFlow en su entorno de demostración de AWS.

**Criterios de aceptación**

- El responsable técnico puede acceder al frontend mediante una URL HTTPS aprobada.
- Se puede completar este recorrido con usuarios de demostración: crear proyecto y tarea como `Admin`, asignarla a `Member`, iniciar sesión como ese `Member`, cambiar el estado y comprobar el panel.
- API, base de datos e identidad desplegadas funcionan sin depender de la máquina local del desarrollador.
- Una guía operativa explica cómo reproducir el despliegue y revertir una entrega.
- Los recursos y el gasto de AWS se aprueban antes del aprovisionamiento.

## 5. Reglas de negocio

| Regla | Descripción |
|---|---|
| BR-001 | Cada tarea pertenece a un proyecto existente. |
| BR-002 | Los nombres de proyecto y títulos de tarea no pueden estar vacíos ni contener solo espacios. |
| BR-003 | La prioridad será Baja, Media o Alta. |
| BR-004 | El estado será Pendiente, En curso o Completada. |
| BR-005 | Solo `Admin` puede crear, editar y archivar proyectos, o crear, editar, eliminar y asignar tareas. |
| BR-006 | `Member` solo puede cambiar el estado de las tareas que tiene asignadas. |
| BR-007 | Las tareas sin asignar se pueden consultar, pero `Member` no puede cambiar su estado. |
| BR-008 | Los proyectos archivados y sus tareas se excluyen de las vistas activas y de los indicadores. |
| BR-009 | Las marcas temporales representan instantes de manera coherente; la visualización tiene una estrategia de zona horaria definida. Si el vencimiento se modela como fecha de calendario, no se convierte en un instante y se compara con la fecha de negocio acordada. |
| BR-010 | Una identidad sin un rol admitido no puede ejecutar operaciones de negocio. |

## 6. Recorridos para la demostración de aceptación

### Recorrido A — `Admin` crea y asigna trabajo

1. Iniciar sesión como `Admin`.
2. Crear el proyecto «Rediseño del sitio web».
3. Crear la tarea «Construir la página de inicio», prioridad Alta y estado Pendiente.
4. Asignarla a «Miembro A» y establecer una fecha de vencimiento futura.
5. Recargar y comprobar que proyecto, tarea y asignación se conservan.

### Recorrido B — `Member` actualiza su trabajo

1. Iniciar sesión como «Miembro A» con rol `Member`.
2. Abrir «Mis tareas» y localizar «Construir la página de inicio».
3. Cambiar el estado a En curso y después a Completada.
4. Recargar y comprobar el cambio en la lista y en los indicadores.
5. Invocar una acción exclusiva de `Admin` y comprobar que la API la rechaza.
6. Intentar cambiar el estado de una tarea ajena o sin asignar y comprobar HTTP 403.

### Recorrido C — Validación y registros inexistentes

1. Intentar crear un proyecto con nombre vacío y comprobar la validación.
2. Abrir la URL de un proyecto inexistente y comprobar el mensaje correspondiente.
3. Intentar crear una tarea para un proyecto inexistente y comprobar que la API la rechaza.

### Recorrido D — Validación en AWS

1. Abrir la aplicación desplegada mediante HTTPS.
2. Autenticarse con los usuarios de demostración configurados.
3. Completar los recorridos A–C con los servicios desplegados.
4. Mostrar las comprobaciones de salud, los registros, la documentación y el flujo de CI utilizado.

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

**Criterio de finalización de un requisito:** permisos aplicados en el backend cuando corresponda, interacción de frontend utilizable, contrato acordado, validación y errores contemplados, verificación documentada y PR aprobadas. Una pantalla con datos simulados no completa un requisito integrado.

## 8. Cuestiones de diseño para la fase 0

Las respuestas deberán proponerse en la arquitectura y revisarse antes de implementar las funcionalidades dependientes. La [visión general del sistema](../03-architecture/system-overview.md) recoge las propuestas iniciales y lo que sigue pendiente.

1. ¿Cómo se mapearán los datos de identidad y roles de Keycloak a los permisos de la aplicación?
2. ¿Cómo se obtendrá la lista de usuarios asignables sin exponer credenciales administrativas de Keycloak al navegador?
3. ¿Cómo se normalizarán errores y mensajes de validación de la API?
4. ¿El borrado de tareas será físico o lógico y por qué?
5. ¿Cómo se publicará un contrato de API que permita trabajar en paralelo?
6. ¿Qué diseño AWS proporcionará HTTPS, secretos, persistencia e identidad con un coste aceptable?
7. ¿Qué pruebas automatizadas y verificaciones básicas demostrarán cada recorrido principal?
8. ¿Qué operaciones se permitirán sobre proyectos archivados y sus tareas? ¿Qué tipo tendrá el vencimiento y qué zona horaria regirá los indicadores?
9. ¿Qué vigencia tendrán los tokens y qué garantías de cierre de sesión se requieren para la demostración?

## 9. Control de cambios y aprobación

- El responsable técnico controla los cambios de requisitos y prioridades.
- Los desarrolladores pueden proponer ajustes mediante incidencias con su impacto en API, interfaz, datos, pruebas y calendario.
- Los cambios aceptados actualizan este documento y sus criterios de aceptación en la misma PR o en una PR de documentación vinculada.
- Todos los requisitos marcados como obligatorios forman parte del MVP. Las mejoras opcionales no retrasarán la funcionalidad requerida ni la entrega en AWS.

**Punto de aprobación:** la fase 0 finalizará cuando el responsable técnico acepte la propuesta de arquitectura, contratos iniciales, modelo de datos y plan de implementación frente a estos requisitos.
