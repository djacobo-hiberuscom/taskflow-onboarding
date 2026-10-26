# TaskFlow — Issues (plan de una semana)

> **Responsables:** David (backend y base de datos) y Oliver (frontend y Keycloak) · **Plan:** [épicas](../epics/README.md)

## Convenciones

- Identificador `ISS-NN`, un archivo por issue, una issue = una PR. Al crearla en GitHub se anota su número en la propia ficha y GitHub Projects es la fuente del estado.
- Cada issue cita los `FR-###` que implementa; la PR la enlaza con `Closes #…` (ramas y PR según [CONTRIBUTING.md](../../CONTRIBUTING.md)).
- Prioridad: **P0** imprescindible · **P1** MVP · **P2** se aplaza si no hay tiempo. Horas: estimación orientativa.
- Estados: Pendiente → Preparado → En curso → En revisión → Finalizado.
- Etiquetas: `epic:EPIC-0N`, `area:backend|frontend|infrastructure|database|identity`, `type:feature|test|bug`, `priority:P0|P1|P2`, `shared`.
- La revisión la hace la otra persona; las conjuntas, el responsable técnico.

## Definición de hecho

- Cumple los criterios de aceptación de la issue y su verificación.
- Incluye pruebas automatizadas de lo que implementa; las de seguridad y visibilidad cubren el caso permitido y el denegado.
- Permisos y visibilidad se aplican en el backend; la interfaz por sí sola no cuenta.
- No añade secretos a Git. No modifica la documentación salvo `api-contracts.md` si cambia el contrato.
- PR revisada e integrada con *squash and merge*.

## Índice

| ID | Título | Épica | Resp. | Prio. | Horas | Día |
|---|---|---|---|:---:|:---:|:---:|
| [ISS-01](ISS-01-apphost-db.md) | AppHost de Aspire con PostgreSQL 18 y roles separados | EPIC-01 | David | P0 | 4 | 1 |
| [ISS-02](ISS-02-backend-base.md) | Solución backend de cuatro capas, API base, EF Core y pruebas | EPIC-01 | David | P0 | 6 | 2 |
| [ISS-03](ISS-03-keycloak-realm.md) | Keycloak en AppHost: realm, clientes y usuarios de demostración | EPIC-01 | Oliver | P0 | 4 | 1 |
| [ISS-04](ISS-04-spa-base.md) | SPA base: Vite, TanStack, shadcn, navegación, cliente tipado y simulaciones | EPIC-01 | Oliver | P0 | 6 | 2 |
| [ISS-05](ISS-05-domain-model.md) | Dominio, modelo EF Core y migración inicial con pruebas del esquema | EPIC-02 | David | P0 | 5 | 2 |
| [ISS-06](ISS-06-projects-api.md) | API de proyectos con visibilidad, pertenencia y bloqueo por proyecto | EPIC-02 | David | P0 | 7 | 4 |
| [ISS-07](ISS-07-tasks-api.md) | API de tareas: CRUD, estado y asignación atómica | EPIC-02 | David | P0 | 7 | 5 |
| [ISS-08](ISS-08-task-queries.md) | Listados de tareas, «Mis tareas» y semilla de demostración | EPIC-02 | David | P0 | 4 | 5 |
| [ISS-09](ISS-09-projects-ui.md) | Pantallas de proyectos: lista, detalle, miembros y acciones de administración | EPIC-02 | Oliver | P0 | 6 | 3 |
| [ISS-10](ISS-10-tasks-ui.md) | Pantallas de tareas: listado con filtros, detalle y formularios | EPIC-02 | Oliver | P0 | 8 | 4 |
| [ISS-11](ISS-11-assign-status-ui.md) | Asignación, cambio de estado y «Mis tareas» | EPIC-02 | Oliver | P0 | 6 | 5 |
| [ISS-12](ISS-12-jwt-auth.md) | JWT de Keycloak, roles y usuario local (GET /me) | EPIC-03 | David | P0 | 4 | 3 |
| [ISS-13](ISS-13-introspection.md) | Introspección por petición y 503 ante fallo del proveedor | EPIC-03 | David | P2 | 2 | 5 |
| [ISS-14](ISS-14-dashboard-api.md) | Endpoint del panel de indicadores | EPIC-03 | David | P1 | 2 | 5 |
| [ISS-15](ISS-15-oidc-session.md) | OIDC con PKCE, sesión, rutas protegidas y controles por rol | EPIC-03 | Oliver | P0 | 6 | 2 |
| [ISS-16](ISS-16-dashboard-profile-ui.md) | Panel de indicadores y Perfil | EPIC-03 | Oliver | P1 | 4 | 5 |
| [ISS-17](ISS-17-integration-demo.md) | Integración final y demostración de los recorridos A, B, C y E | EPIC-03 | Ambos | P0 | 3 | 5 |

## Trazabilidad de requisitos

| Requisito | Issues |
|---|---|
| FR-001 | [ISS-03](ISS-03-keycloak-realm.md), [ISS-12](ISS-12-jwt-auth.md), [ISS-15](ISS-15-oidc-session.md) |
| FR-002 | [ISS-13](ISS-13-introspection.md), [ISS-15](ISS-15-oidc-session.md), [ISS-16](ISS-16-dashboard-profile-ui.md) |
| FR-003 | [ISS-03](ISS-03-keycloak-realm.md), [ISS-12](ISS-12-jwt-auth.md), [ISS-15](ISS-15-oidc-session.md), [ISS-16](ISS-16-dashboard-profile-ui.md) |
| FR-004 | [ISS-06](ISS-06-projects-api.md), [ISS-09](ISS-09-projects-ui.md) |
| FR-005 | [ISS-06](ISS-06-projects-api.md), [ISS-09](ISS-09-projects-ui.md) |
| FR-006 | [ISS-06](ISS-06-projects-api.md), [ISS-09](ISS-09-projects-ui.md) |
| FR-007 | [ISS-06](ISS-06-projects-api.md), [ISS-09](ISS-09-projects-ui.md) |
| FR-008 | [ISS-06](ISS-06-projects-api.md), [ISS-09](ISS-09-projects-ui.md) |
| FR-009 | [ISS-07](ISS-07-tasks-api.md), [ISS-10](ISS-10-tasks-ui.md) |
| FR-010 | [ISS-07](ISS-07-tasks-api.md), [ISS-10](ISS-10-tasks-ui.md) |
| FR-011 | [ISS-07](ISS-07-tasks-api.md), [ISS-10](ISS-10-tasks-ui.md) |
| FR-012 | [ISS-07](ISS-07-tasks-api.md), [ISS-10](ISS-10-tasks-ui.md) |
| FR-013 | [ISS-05](ISS-05-domain-model.md), [ISS-07](ISS-07-tasks-api.md), [ISS-11](ISS-11-assign-status-ui.md) |
| FR-014 | [ISS-07](ISS-07-tasks-api.md), [ISS-11](ISS-11-assign-status-ui.md) |
| FR-015 | [ISS-08](ISS-08-task-queries.md), [ISS-10](ISS-10-tasks-ui.md) |
| FR-016 | [ISS-08](ISS-08-task-queries.md), [ISS-11](ISS-11-assign-status-ui.md) |
| FR-017 | [ISS-14](ISS-14-dashboard-api.md), [ISS-16](ISS-16-dashboard-profile-ui.md) |
| FR-018 | [ISS-04](ISS-04-spa-base.md), [ISS-17](ISS-17-integration-demo.md) |
| FR-019 | [ISS-01](ISS-01-apphost-db.md), [ISS-05](ISS-05-domain-model.md), [ISS-17](ISS-17-integration-demo.md) |
| FR-020 | [ISS-08](ISS-08-task-queries.md) |
| FR-021 | [ISS-05](ISS-05-domain-model.md), [ISS-06](ISS-06-projects-api.md), [ISS-09](ISS-09-projects-ui.md) |
| FR-022 | [ISS-06](ISS-06-projects-api.md), [ISS-09](ISS-09-projects-ui.md) |
| FR-023 | [ISS-06](ISS-06-projects-api.md), [ISS-07](ISS-07-tasks-api.md), [ISS-09](ISS-09-projects-ui.md), [ISS-11](ISS-11-assign-status-ui.md) |
