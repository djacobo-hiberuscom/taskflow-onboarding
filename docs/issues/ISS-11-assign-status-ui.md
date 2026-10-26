# ISS-11 — Asignación, cambio de estado y «Mis tareas»

| Campo | Valor |
|---|---|
| **Épica** | [EPIC-02](../epics/EPIC-02-mvp-proyectos-y-tareas.md) — MVP de proyectos y tareas |
| **Responsable** | Oliver |
| **Revisión** | David |
| **Prioridad** | P0 — imprescindible |
| **Estimación** | ≈ 6 h |
| **Día objetivo** | 5 |
| **Requisitos** | FR-013, FR-014, FR-016, FR-023 |
| **Etiquetas** | `epic:EPIC-02` `area:frontend` `type:feature` `priority:P0` |
| **Estado** | Pendiente |
| **Issue de GitHub** | [#13](https://github.com/djacobo-hiberuscom/taskflow-onboarding/issues/13) |
| **Rama sugerida** | `feature/ISS-11-assign-status-ui` |

## Contexto

Estado y asignación tienen operaciones separadas; asignar a un no miembro debe avisar.

## Alcance

- [ ] Selector de asignables con «sin asignar» y aviso de que la persona pasará a ver el proyecto.
- [ ] Cambio de estado entre las tres opciones con invalidación de consultas.
- [ ] Página MisTask con estado vacío.

## Criterios de aceptación

- [ ] El aviso solo aparece para no miembros.
- [ ] El estado y el asignado se conservan al recargar.
- [ ] «Mis tareas» muestra solo las del usuario autenticado.

## Dependencias

**Bloqueada por:** [ISS-10](ISS-10-tasks-ui.md) (Pantallas de tareas: listado con filtros, detalle y formularios)

**Bloquea a:** [ISS-17](ISS-17-integration-demo.md)

## Verificación

Recorridos A y B manuales.

## Referencias

- [03-architecture/frontend.md](../03-architecture/frontend.md)

Aplica la [definición de hecho](README.md#definición-de-hecho).
