# TaskFlow — Arquitectura del backend

> **Responsable:** David — backend
>
> **Estado:** borrador de fase 0; propuesta pendiente de revisión. No hay código de backend ni migraciones implementadas.
>
> **Referencia:** copia de Documentos, especificación revisada el 2026-10-22. Roles `Admin` y `User`, visibilidad por pertenencia y gestión de tareas por los miembros del proyecto.

## 1. Propósito y documentos relacionados

Este documento describe las responsabilidades, capas, dependencias, casos de uso, autorización y persistencia de la API. Toma como referencia la especificación nueva, confirmada durante la revisión, y no la matriz anterior de `Admin`/`Member` con lectura global.

- [Requisitos funcionales](../02-requirements/functional-requirements.md): comportamiento exigido y criterios de aceptación.
- [Visión general](system-overview.md): componentes y propuesta de separación en capas.
- [Modelo de datos](database.md): `AppUser`, `Project`, `AppUser_Project`, `Task` y restricciones.
- [Autenticación](authentication.md): OIDC, JWT y mapeo de roles.
- [Contrato de API](../05-api/api-contracts.md): rutas, DTO y respuestas propuestas.
- [Frontend](frontend.md): integración, sesión, consultas y formularios.
- [ADR](adr/README.md): decisiones propuestas y su aprobación.

Se distingue entre comportamiento definido por los requisitos y mecanismos técnicos todavía propuestos. Las decisiones sin resolver se enumeran en §12; este borrador no las considera aprobadas.

## 2. Tecnologías y alcance

La base prevista es C#, ASP.NET Core, Entity Framework Core, PostgreSQL y Keycloak. Las versiones compatibles de .NET, EF Core, el proveedor de PostgreSQL y los contenedores se elegirán conjuntamente antes de implementar.

El backend es una única API desplegable. No se requieren microservicios, colas, CQRS, mediadores ni repositorios genéricos para cumplir el MVP. Se acuerda organizarlo en cuatro proyectos: `TaskFlow.Api`, `TaskFlow.Application`, `TaskFlow.Domain` y `TaskFlow.Infrastructure`, siguiendo la separación de Clean Architecture. Queda pendiente registrar esta decisión en un ADR; no implica adoptar DDD completo.

## 3. Capas, responsabilidades y dependencias

| Proyecto acordado | Responsabilidad | Dependencias |
|---|---|---|
| `TaskFlow.Domain` | Entidades, valores admitidos y reglas de negocio que no dependen del transporte ni de la persistencia. | Ninguna de las otras capas; sin ASP.NET Core, EF Core o Keycloak. |
| `TaskFlow.Application` | Casos de uso, comprobaciones por recurso, coordinación de transacciones y contratos de acceso a datos e identidad actual. | `Domain`. |
| `TaskFlow.Infrastructure` | EF Core, configuración de entidades, PostgreSQL, migraciones e implementación de contratos de persistencia. | `Application` y `Domain`. |
| `TaskFlow.Api` | Endpoints, DTO HTTP, validación de entrada, autenticación JWT, mapeo de roles, políticas y traducción de resultados a HTTP. | `Application`; `Infrastructure` para registrar implementaciones en el arranque. |

Los endpoints llaman a los casos de uso; no consultan directamente la base de datos. `Application` recibe la identidad actual mediante un contrato, sin depender de `HttpContext` ni de la estructura del token de Keycloak. Las entidades de `Domain` no contienen DTO HTTP ni conocen los roles anidados del proveedor de identidad.

Los detalles del acceso a datos desde `Application` —contratos por caso de uso, repositorios específicos u otra alternativa— quedan pendientes. No se introduce por defecto una abstracción genérica para cada tabla.

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

La API recibe el access token en `Authorization: Bearer ...` y valida firma, emisor, audiencia y vigencia con JWT Bearer. Un ID token no se admite como credencial de acceso a la API. La API responde con códigos HTTP; la SPA gestiona las redirecciones a Keycloak.

Se mantiene como propuesta de [authentication.md](authentication.md) extraer únicamente `Admin` y `User` de `realm_access.roles` mediante una transformación de reclamaciones. La transformación debe evitar duplicar reclamaciones si se ejecuta varias veces y configurar explícitamente el tipo de reclamación utilizado como rol. Los nombres definitivos de realm, clientes y audiencia, los tiempos de sesión y la vigencia residual siguen pendientes de aprobación.

### Usuario local

La identidad se obtiene del `sub` validado y se resuelve mediante `AppUser.keycloak_sub`. Para nuevos usuarios registrados con un rol admitido, se prevé crear la referencia local en su primera solicitud. La unicidad de `keycloak_sub` debe permitir recuperar la misma fila ante solicitudes simultáneas, sin crear duplicados ni fallar por una carrera esperable.

Los usuarios de demostración pueden precargarse para poder asignarles tareas antes de su primer acceso. Los usuarios registrados que aún no tienen referencia local no aparecen en la lista de asignables, conforme a la propuesta actual de autenticación.

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
| Token ausente, inválido o caducado | 401. |
| Token válido sin `Admin` ni `User` | 403. |
| Detalle de un recurso inexistente o no visible para `User` | 404, conforme a BR-015; otros documentos aún lo marcan como pendiente. |
| `User` intenta una operación administrativa sobre un proyecto visible | 403. |
| Lista o panel sin proyectos visibles | 200 con lista vacía o recuentos cero. No 404 por el mero hecho de no tener pertenencias. |

Hay que acordar la precedencia en las operaciones administrativas sobre recursos no visibles y archivados. Una política `AdminOnly` aplicada directamente al endpoint puede devolver 403 antes de comprobar el recurso, mientras el contrato propone 404 cuando no es visible. Si se conserva esa distinción, la comprobación debe coordinarse con el caso de uso o un mecanismo de autorización por recurso. No debe depender accidentalmente del orden de los middleware.

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

La autorización, comprobación de estado del proyecto y escritura deben ser coherentes dentro de la operación. Falta decidir cómo resolver carreras entre un archivado y una modificación por `User`, además de las ediciones simultáneas de una misma tarea (§12).

No se incorporan endpoints de retirada manual de miembros ni de eliminación de usuarios al MVP. Si se elimina un usuario local por otro procedimiento, se bloqueará la operación completa cuando sea el último miembro de algún proyecto, activo o archivado, y se informará de que debe incorporarse otro miembro antes de eliminarlo. Si se permite el borrado, sus tareas se conservan sin asignar. La comprobación y los cambios deben ser atómicos y protegerse ante borrados concurrentes; el mecanismo concreto sigue pendiente.

Esta regla no bloquea eliminar el proyecto: `Admin` puede borrar un proyecto activo o archivado en cualquier momento, aunque tenga un único miembro, con las cascadas ya previstas.

## 7. Persistencia y migraciones

`Infrastructure` implementará el acceso con EF Core y el proveedor de PostgreSQL elegido. El mapeo seguirá [database.md](database.md): claves primarias y únicas, pertenencia N:M, autor opcional tras su eliminación, tareas con proyecto obligatorio y asignación opcional.

La FK compuesta de asignación debe conservar `project_id` al eliminar la pertenencia y dejar solo `assigned_id` a `NULL`. PostgreSQL 15 documenta `ON DELETE SET NULL (assigned_id)`; falta comprobar el SQL generado por la combinación elegida de EF Core y proveedor. Un `DeleteBehavior.SetNull` genérico sobre toda la clave compuesta no debe darse por equivalente.

Se configurará el mismo orden de propiedades en la clave principal y en la FK compuesta. El borrador usa PK de pertenencia (`user_id`, `project_id`) y referencia (`project_id`, `assigned_id`); el mapeo debe alinear ese orden o definir expresamente la clave principal alternativa que corresponda.

Las migraciones deberán probarse desde una base vacía y con datos de ejemplo. Se comprobarán especialmente la tarea sin asignar, la asignación a miembro, la eliminación local de usuario y la cascada de un proyecto con tareas asignadas. La alternativa de desasignar desde un caso de uso no protege un borrado directo en SQL; cualquier alternativa debe documentar qué garantiza la base y qué garantiza la aplicación.

Los tipos de identificador, longitudes, restricciones de estado y prioridad y posible nombre definitivo `due_date` siguen pendientes. Las migraciones de negocio no modifican las tablas internas de Keycloak.

## 8. Contrato HTTP, consultas y fechas

Se sigue el borrador de [api-contracts.md](../05-api/api-contracts.md): prefijo `/api/v1`, JSON con campos `camelCase`, DTO específicos, instantes UTC y vencimiento como fecha de calendario. Las entidades de EF Core no se exponen directamente en las respuestas.

- Filtrar por visibilidad antes de contar, ordenar y paginar. El cliente no puede ampliar su acceso cambiando `projectId` o `assigneeId`.
- La paginación de proyectos y tareas está decidida; los tamaños predeterminados y máximos siguen propuestos.
- Los campos de ordenación se limitan a los admitidos. Orden predeterminado, desempate y posición de vencimientos nulos se concretarán con frontend.
- «Mis tareas» deriva el usuario del token y conserva el filtro de proyectos activos.
- Los recuentos del panel se calculan sobre todos los registros relevantes, no solo la página solicitada.
- La API calcula `overdueTasks`, `businessDate` y `businessTimeZone` para el panel usando `Europe/Madrid`. El frontend calcula `isOverdue` para cada tarea; el atributo no se persiste ni se envía en el DTO de tarea.
- Las marcas temporales se generan en backend en UTC. Una fecha de vencimiento no se convierte automáticamente en un instante UTC.

Se propone un reloj inyectable para poder probar cambios de día y límites de fecha sin depender de la hora real del equipo. La biblioteca o mecanismo concreto se elegirá con la versión de .NET.

### Validación y errores

El backend valida entradas y reglas aunque el frontend ya las compruebe. Nombres y títulos no pueden estar vacíos tras recortar espacios; estados, prioridades, fechas, filtros y parámetros de paginación deben ser válidos.

Se mantiene la propuesta de Problem Details y `ValidationProblemDetails`, con errores por campo en `es-ES` y un identificador de correlación para errores inesperados. No se exponen consultas SQL, credenciales ni trazas internas al cliente.

El contrato todavía mezcla ignorar propiedades protegidas y rechazar propiedades desconocidas; debe elegirse una regla antes de implementar. Los DTO nunca deben permitir modificar `created_by`, pertenencias o archivado mediante campos de la edición general.

## 9. Solicitudes duplicadas y reintentos

Para FR-018, el frontend bloquea nuevos envíos del formulario mientras la operación está pendiente. Se mantiene la decisión de no añadir claves de idempotencia ni deduplicación general de creaciones al backend en el MVP.

Por tanto, no debe configurarse un reintento automático indiscriminado de escrituras ante fallos de red: una petición de creación puede haberse confirmado en la base aunque se pierda su respuesta. Los reintentos de lecturas y los de operaciones concretas que sí sean idempotentes se configurarán de forma diferenciada.

La unicidad de usuarios locales y pertenencias evita duplicados estructurales durante sus altas concurrentes y sigue siendo necesaria. Es distinta de garantizar que repetir un `POST` de creación produzca un único recurso.

## 10. Configuración y operación

La API tendrá configuración por entorno para conexión a PostgreSQL, emisor de Keycloak, audiencia, orígenes CORS, zona de negocio y registros. Los nombres definitivos de las variables y los puertos siguen pendientes de la guía de entorno.

- Credenciales fuera de Git; valores de ejemplo sin secretos en la documentación de entorno.
- CORS limitado a los orígenes aprobados de la SPA. No sustituye autenticación ni autorización.
- En Docker, la URL de metadatos accesible por la API y el emisor público deben resolverse sin desactivar la validación de `iss`.
- Registros de operaciones y errores con identificadores de correlación, sin tokens completos ni contraseñas.
- `/health` comprobará la salud prevista en el contrato sin publicar detalles sensibles de conexión.
- Migraciones y datos iniciales tendrán un procedimiento explícito, reproducible y separado de la autenticación de usuarios.

El despliegue AWS depende del documento conjunto `deployment-aws.md`, que está vacío en la versión revisada. Aquí no se eligen servicios, presupuesto ni estrategia de despliegue por defecto.

## 11. Verificación prevista

No existen pruebas ejecutables todavía. Se propone xUnit para reglas y pruebas de integración de ASP.NET Core, con PostgreSQL real cuando se comprueben relaciones, cascadas o consultas específicas.

Cobertura mínima:

- JWT ausente o inválido, emisor/audiencia/firma incorrectos, caducidad y roles no admitidos.
- Recorridos A–E de la especificación nueva y permisos de `Admin`/`User` sobre activos y archivados.
- Visibilidad en detalles, listas, filtros y panel; «Mis tareas» sin aceptar identidad ajena.
- Creación de proyecto con pertenencia del autor y asignación con alta automática de miembro, sin cambios parciales.
- Altas simultáneas de `AppUser` y de pertenencia sin duplicados ni errores por carreras esperables.
- Borrado físico, cascadas y conservación del proyecto al dejar una tarea sin asignar.
- Rechazo íntegro del borrado de un usuario que sea el último miembro de algún proyecto, incluidos archivados; protección ante borrados concurrentes y posibilidad de eliminar el propio proyecto con un único miembro.
- Paginación, ordenación, filtros y recuentos sobre el conjunto completo autorizado.
- Vencimientos en `Europe/Madrid`: ayer, hoy, sin vencimiento y tarea completada.
- Migraciones desde base vacía y casos de FK compuesta con columnas opcionales.

La colección de Bruno documentada en el contrato permitirá comprobaciones manuales reproducibles. No sustituye las pruebas automatizadas. La CI compilará y probará el backend; su configuración concreta sigue pendiente.

## 12. Decisiones pendientes y observaciones de revisión

Las siguientes cuestiones deben cerrarse o registrarse expresamente como limitaciones antes de implementar la funcionalidad afectada. Las propuestas de otros documentos no se convierten en decisiones aceptadas por aparecer en este borrador.

| Cuestión | Qué falta decidir o corregir | Documento relacionado |
|---|---|---|
| Versiones | Elegir versiones compatibles y con soporte. La estructura de cuatro proyectos ya está acordada; queda registrarla en un ADR. | Visión general, ADR nuevo. |
| Acceso a datos y validación | Concretar los contratos de persistencia de `Application` y el mecanismo de validación. El uso de Controllers ya está acordado. | Este documento. |
| Protección del último miembro | Regla decidida: bloquear íntegramente el borrado de un usuario si dejaría algún proyecto sin miembros. Falta elegir el mecanismo transaccional o de base de datos y comprobar carreras, sin bloquear la eliminación del propio proyecto por `Admin`. | Modelo de datos, este documento. |
| Visibilidad y códigos HTTP | BR-015 fija 404, pero sigue figurando pendiente en otros apartados. Acordar precedencia con `AdminOnly`; listar archivados no selecciona un proyecto concreto. Las listas y el panel sin resultados deben seguir devolviendo 200. | Requisitos §8, autenticación, contrato. |
| Concurrencia de ediciones | Última escritura gana o versión. Si se adopta `ETag`/`If-Match`, una precondición incumplida corresponde a 412; el contrato propone 409. Un conflicto de versión en el cuerpo puede definirse como 409. | Contrato §9, modelo de datos. |
| Carreras entre operaciones | Definir garantías al archivar mientras `User` modifica y al eliminar/asignar simultáneamente; estrategia transaccional y pruebas. | Este documento, modelo de datos. |
| Campos desconocidos y estado inicial | Elegir entre ignorar o rechazar propiedades no admitidas. Resolver la obligatoriedad de `status` frente al valor por defecto propuesto. | Contrato §5, FR-009. |
| Modelo físico y migración | Tipos, longitudes, estados/prioridades, orden de claves compuestas y generación del `SET NULL` parcial. | Modelo de datos, ADR. |
| Autenticación | Nombres definitivos, vigencias, aceptación de vigencia residual, resolución de emisor local y sincronización de nombre/correo del usuario. | Autenticación. |
| Consultas | Tamaños de página, ordenación, desempate y formato final del OpenAPI versionado. | Contrato. |
| Reintentos y actualización temporal | Distinguir reintentos de lecturas y escrituras; concretar cómo refrescar vencimientos al cambiar el día. | Frontend. |
| Preparación y despliegue | Variables, puertos, comandos, ejecución de migraciones, CI y completar la propuesta AWS. | Guías de desarrollo, despliegue AWS. |

La pertenencia se conserva al desasignar según FR-023 y ADR-0001. Su retirada automática es una cuestión futura, no un comportamiento que deba implementarse mientras siga expresamente fuera del MVP. El borrado local de usuarios tampoco forma parte de los endpoints previstos.

## 13. Referencias técnicas de la revisión

- [ASP.NET Core: autenticación JWT Bearer](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication).
- [ASP.NET Core: tratamiento de errores de API](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling-api).
- [EF Core: claves foráneas y principales](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/foreign-and-principal-keys), incluido el orden de las propiedades en claves compuestas.
- [PostgreSQL 15: restricciones y acciones de claves foráneas](https://www.postgresql.org/docs/15/ddl-constraints.html), incluido `SET NULL` sobre un subconjunto de columnas.
- [RFC 9110, §13.1.1: If-Match](https://www.rfc-editor.org/rfc/rfc9110.html#name-if-match).

