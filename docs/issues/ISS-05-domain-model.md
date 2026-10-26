# ISS-05 — Dominio, modelo EF Core y migración inicial con pruebas del esquema

| Campo | Valor |
|---|---|
| **Épica** | [EPIC-02](../epics/EPIC-02-mvp-proyectos-y-tareas.md) — MVP de proyectos y tareas |
| **Responsable** | David |
| **Revisión** | Oliver |
| **Prioridad** | P0 — imprescindible |
| **Estimación** | ≈ 5 h |
| **Día objetivo** | 2 |
| **Requisitos** | FR-019, FR-013, FR-021 |
| **Etiquetas** | `epic:EPIC-02` `area:database` `type:feature` `priority:P0` |
| **Estado** | Pendiente |
| **Issue de GitHub** | #— |
| **Rama sugerida** | `feature/ISS-05-domain-model` |

## Contexto

El esquema soporta la regla de que una tarea solo se asigna a un miembro.

## Alcance

- [ ] Entidades, enums y reglas BR-001 a BR-004 y BR-013 en `Domain`.
- [ ] Mapeo de `AppUser`, `Project`, `AppUser_Project` y `Task` con `CHECK`, índices y FK compuesta `ON DELETE SET NULL (assigned_id)`; cascadas.
- [ ] Migración inicial revisada sobre base vacía y pruebas de FK compuesta y cascadas en PostgreSQL.

## Criterios de aceptación

- [ ] La migración se aplica en una base vacía y el SQL coincide con `database.md`.
- [ ] Una tarea no se asigna a un no miembro.
- [ ] Eliminar un proyecto no deja huérfanos.

## Dependencias

**Bloqueada por:** [ISS-02](ISS-02-backend-base.md) (Solución backend de cuatro capas, API base, EF Core y pruebas)

**Bloquea a:** [ISS-06](ISS-06-projects-api.md)

## Verificación

`dotnet test` sobre Testcontainers.

## Referencias

- [03-architecture/database.md](../03-architecture/database.md)
- [03-architecture/backend.md](../03-architecture/backend.md)

Aplica la [definición de hecho](README.md#definición-de-hecho).
