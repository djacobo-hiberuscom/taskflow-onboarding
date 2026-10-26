# ISS-04 — SPA base: Vite, TanStack, shadcn, navegación, cliente tipado y simulaciones


| Campo | Valor |
|---|---|
| **Épica** | [EPIC-01](../epics/EPIC-01-entorno-y-bases.md) — Entorno Aspire y bases técnicas |
| **Responsable** | Oliver |
| **Revisión** | David |
| **Prioridad** | P0 — imprescindible |
| **Estimación** | ≈ 6 h |
| **Día objetivo** | 2 |
| **Requisitos** | FR-018 |
| **Etiquetas** | `epic:EPIC-01` `area:frontend` `type:feature` `priority:P0` |
| **Estado** | Pendiente |
| **Issue de GitHub** | [#6](https://github.com/djacobo-hiberuscom/taskflow-onboarding/issues/6) |
| **Rama sugerida** | `feature/ISS-04-spa-base` |

## Contexto

Estructura para trabajar contra el contrato mientras el backend se completa.

## Alcance

- [ ] `frontend/` con Vite, React, TypeScript, TanStack Router y Query, Tailwind y shadcn/ui; Vitest con RTL; registrar Vite en AppHost con variables `VITE_*`.
- [ ] Providers, layouts, rutas de `frontend.md` §3.7 con páginas esqueleto y páginas 403, 404 y Timeout.
- [ ] Cliente HTTP tipado desde `api-contracts.md` (o `openapi.json` cuando exista), mapeo de errores, MSW, `query-keys.ts`, `dates.ts` con `isOverdue` en `Europe/Madrid`.
- [ ] Componentes comunes: confirmación, vacío, error, carga y badges.

## Criterios de aceptación

- [ ] `dev`, `build`, `lint` y `test` funcionan en limpio.
- [ ] `isOverdue` cubre ayer, hoy, sin fecha y completada.
- [ ] Ninguna variable `VITE_*` contiene secretos.

## Dependencias

**Bloqueada por:** [ISS-01](ISS-01-apphost-db.md) (AppHost de Aspire con PostgreSQL 18 y roles separados)

**Bloquea a:** [ISS-09](ISS-09-projects-ui.md), [ISS-10](ISS-10-tasks-ui.md), [ISS-15](ISS-15-oidc-session.md)

## Verificación

Ejecutar los scripts y las pruebas de `dates.ts`.

## Referencias

- [03-architecture/frontend.md](../03-architecture/frontend.md)
- [05-api/api-contracts.md](../05-api/api-contracts.md)

Aplica la [definición de hecho](README.md#definición-de-hecho).
