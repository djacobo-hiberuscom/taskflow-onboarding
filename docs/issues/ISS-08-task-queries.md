# ISS-08 — Listados de tareas, «Mis tareas» y semilla de demostración

| Campo | Valor |
|---|---|
| **Épica** | [EPIC-02](../epics/EPIC-02-mvp-proyectos-y-tareas.md) — MVP de proyectos y tareas |
| **Responsable** | David |
| **Revisión** | Oliver |
| **Prioridad** | P0 — imprescindible |
| **Estimación** | ≈ 4 h |
| **Día objetivo** | 5 |
| **Requisitos** | FR-015, FR-016, FR-020 |
| **Etiquetas** | `epic:EPIC-02` `area:backend` `type:feature` `priority:P0` |
| **Estado** | Pendiente |
| **Issue de GitHub** | [#10](https://github.com/djacobo-hiberuscom/taskflow-onboarding/issues/10) |
| **Rama sugerida** | `feature/ISS-08-task-queries` |

## Contexto

Búsqueda de trabajo con filtros y ordenación calculados en PostgreSQL.

## Alcance

- [ ] `GET /tasks` con filtros (incluido sin asignar), orden por vencimiento o creación y paginación con `totalCount` real.
- [ ] `GET /me/tasks` con identidad del token y solo proyectos activos.
- [ ] Semilla idempotente de `AppUser` con los `sub` del realm.

## Criterios de aceptación

- [ ] Los filtros solo devuelven datos visibles.
- [ ] «Mis tareas» no admite identidad ajena.
- [ ] La semilla ejecutada dos veces no duplica.

## Dependencias

**Bloqueada por:** [ISS-07](ISS-07-tasks-api.md) (API de tareas: CRUD, estado y asignación atómica)

**Bloquea a:** [ISS-17](ISS-17-integration-demo.md)

## Verificación

Pruebas de integración y semilla ejecutada dos veces.

## Referencias

- [05-api/api-contracts.md](../05-api/api-contracts.md)
- [03-architecture/database.md](../03-architecture/database.md)

Aplica la [definición de hecho](README.md#definición-de-hecho).
