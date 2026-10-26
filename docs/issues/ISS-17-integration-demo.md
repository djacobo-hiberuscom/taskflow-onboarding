# ISS-17 — Integración final y demostración de los recorridos A, B, C y E

| Campo | Valor |
|---|---|
| **Épica** | [EPIC-03](../epics/EPIC-03-seguridad-panel-e-integracion.md) — Seguridad, panel e integración final |
| **Responsable** | David + Oliver |
| **Revisión** | Responsable técnico |
| **Prioridad** | P0 — imprescindible |
| **Estimación** | ≈ 3 h |
| **Día objetivo** | 5 |
| **Requisitos** | FR-018, FR-019 |
| **Etiquetas** | `epic:EPIC-03` `area:backend` `type:test` `priority:P0` `shared` |
| **Estado** | Pendiente |
| **Issue de GitHub** | #— |
| **Rama sugerida** | `chore/ISS-17-integration-demo` |

## Contexto

Cierra la semana: SPA real contra API real con Keycloak.

## Alcance

- [ ] Desactivar las simulaciones y comprobar que el bundle no contiene secretos.
- [ ] Ejecutar los recorridos A, B, C y E, incluidas llamadas directas a la API que deben denegarse, y comprobar persistencia tras reiniciar.
- [ ] Corregir los defectos bloqueantes y listar los no resueltos.

## Criterios de aceptación

- [ ] Los cuatro recorridos pasan con Keycloak y la API reales.
- [ ] Las denegaciones devuelven 401/403/404 según el contrato.
- [ ] Los defectos pendientes están listados como issues.

## Dependencias

**Bloqueada por:** [ISS-07](ISS-07-tasks-api.md) (API de tareas: CRUD, estado y asignación atómica), [ISS-08](ISS-08-task-queries.md) (Listados de tareas, «Mis tareas» y semilla de demostración), [ISS-11](ISS-11-assign-status-ui.md) (Asignación, cambio de estado y «Mis tareas»), [ISS-16](ISS-16-dashboard-profile-ui.md) (Panel de indicadores y Perfil), [ISS-09](ISS-09-projects-ui.md) (Pantallas de proyectos: lista, detalle, miembros y acciones de administración), [ISS-10](ISS-10-tasks-ui.md) (Pantallas de tareas: listado con filtros, detalle y formularios)

**Bloquea a:** ninguna.

## Verificación

Demostración en local.

## Referencias

- [02-requirements/functional-requirements.md](../02-requirements/functional-requirements.md)

Aplica la [definición de hecho](README.md#definición-de-hecho).
