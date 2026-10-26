# ISS-10 — Pantallas de tareas: listado con filtros, detalle y formularios

| Campo | Valor |
|---|---|
| **Épica** | [EPIC-02](../epics/EPIC-02-mvp-proyectos-y-tareas.md) — MVP de proyectos y tareas |
| **Responsable** | Oliver |
| **Revisión** | David |
| **Prioridad** | P0 — imprescindible |
| **Estimación** | ≈ 8 h |
| **Día objetivo** | 4 |
| **Requisitos** | FR-009, FR-010, FR-011, FR-012, FR-015 |
| **Etiquetas** | `epic:EPIC-02` `area:frontend` `type:feature` `priority:P0` |
| **Estado** | Pendiente |
| **Issue de GitHub** | #— |
| **Rama sugerida** | `feature/ISS-10-tasks-ui` |

## Contexto

Gestión completa de tareas desde el proyecto y desde el listado.

## Alcance

- [ ] Página Tareas con filtros por proyecto, estado, prioridad y asignado (incluido sin asignar), orden y paginación en la URL.
- [ ] DetailTask y popups NuevaTarea y EditarTarea con validación; eliminar con confirmación.
- [ ] Vencimiento como fecha de calendario sin conversión de zona.

## Criterios de aceptación

- [ ] Un enlace con filtros se recarga sin perderlos.
- [ ] La tarea creada aparece en su proyecto.
- [ ] El vencimiento no se desplaza un día.

## Dependencias

**Bloqueada por:** [ISS-04](ISS-04-spa-base.md) (SPA base: Vite, TanStack, shadcn, navegación, cliente tipado y simulaciones)

**Bloquea a:** [ISS-11](ISS-11-assign-status-ui.md), [ISS-17](ISS-17-integration-demo.md)

## Verificación

Pruebas de formularios y de la lógica de filtros.

## Referencias

- [03-architecture/frontend.md](../03-architecture/frontend.md)
- [05-api/api-contracts.md](../05-api/api-contracts.md)

Aplica la [definición de hecho](README.md#definición-de-hecho).
