# ISS-02 — Solución backend de cuatro capas, API base, EF Core y pruebas

| Campo | Valor |
|---|---|
| **Épica** | [EPIC-01](../epics/EPIC-01-entorno-y-bases.md) — Entorno Aspire y bases técnicas |
| **Responsable** | David |
| **Revisión** | Oliver |
| **Prioridad** | P0 — imprescindible |
| **Estimación** | ≈ 6 h |
| **Día objetivo** | 2 |
| **Requisitos** | — |
| **Etiquetas** | `epic:EPIC-01` `area:backend` `type:feature` `priority:P0` |
| **Estado** | Pendiente |
| **Issue de GitHub** | #— |
| **Rama sugerida** | `feature/ISS-02-backend-base` |

## Contexto

Estructura y herramientas sobre las que se implementa todo el backend.

## Alcance

- [ ] Proyectos `Domain`, `Application`, `Infrastructure`, `Api` y de pruebas con las dependencias de `backend.md` §3.
- [ ] Controllers, `/api/v1`, `/health` con comprobación de base de datos, Problem Details y OpenAPI.
- [ ] EF Core con `TaskFlowDbContext` y comando explícito de migraciones (sin `Migrate` en el arranque).
- [ ] Fixture de Testcontainers con PostgreSQL 18.6 y `WebApplicationFactory`.

## Criterios de aceptación

- [ ] `dotnet build` y `dotnet test` pasan en limpio.
- [ ] `/health` devuelve 200 con base y 503 sin ella.
- [ ] `Domain` no depende de ASP.NET Core ni de EF Core.

## Dependencias

**Bloqueada por:** [ISS-01](ISS-01-apphost-db.md) (AppHost de Aspire con PostgreSQL 18 y roles separados)

**Bloquea a:** [ISS-05](ISS-05-domain-model.md), [ISS-12](ISS-12-jwt-auth.md)

## Verificación

`dotnet test` con Docker.

## Referencias

- [03-architecture/backend.md](../03-architecture/backend.md)
- [05-api/api-contracts.md](../05-api/api-contracts.md)

Aplica la [definición de hecho](README.md#definición-de-hecho).
