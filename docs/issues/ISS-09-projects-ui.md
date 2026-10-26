# ISS-09 — Pantallas de proyectos: lista, detalle, miembros y acciones de administración

| Campo | Valor |
|---|---|
| **Épica** | [EPIC-02](../epics/EPIC-02-mvp-proyectos-y-tareas.md) — MVP de proyectos y tareas |
| **Responsable** | Oliver |
| **Revisión** | David |
| **Prioridad** | P0 — imprescindible |
| **Estimación** | ≈ 6 h |
| **Día objetivo** | 3 |
| **Requisitos** | FR-004, FR-005, FR-006, FR-007, FR-008, FR-021, FR-022, FR-023 |
| **Etiquetas** | `epic:EPIC-02` `area:frontend` `type:feature` `priority:P0` |
| **Estado** | Pendiente |
| **Issue de GitHub** | #— |
| **Rama sugerida** | `feature/ISS-09-projects-ui` |

## Contexto

Pantalla principal de los recorridos A y E.

## Alcance

- [ ] Proyectos con lista paginada, estado vacío y popups NuevoProyecto y EditarProyecto.
- [ ] DetailProyecto con miembros, tareas y marca de archivado.
- [ ] Confirmaciones de archivar, restaurar y eliminar (advierte del borrado de tareas) y popup Archivados.

## Criterios de aceptación

- [ ] El proyecto creado aparece de inmediato y tras recargar.
- [ ] Nombre vacío no se envía y muestra el error.
- [ ] Un 404 de la API muestra la página de no encontrado.

## Dependencias

**Bloqueada por:** [ISS-04](ISS-04-spa-base.md) (SPA base: Vite, TanStack, shadcn, navegación, cliente tipado y simulaciones)

**Bloquea a:** [ISS-17](ISS-17-integration-demo.md)

## Verificación

Pruebas de interacción y recorrido manual.

## Referencias

- [03-architecture/frontend.md](../03-architecture/frontend.md)
- [05-api/api-contracts.md](../05-api/api-contracts.md)

Aplica la [definición de hecho](README.md#definición-de-hecho).
