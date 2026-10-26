# ISS-01 — AppHost de Aspire con PostgreSQL 18 y roles separados

| Campo | Valor |
|---|---|
| **Épica** | [EPIC-01](../epics/EPIC-01-entorno-y-bases.md) — Entorno Aspire y bases técnicas |
| **Responsable** | David |
| **Revisión** | Oliver |
| **Prioridad** | P0 — imprescindible |
| **Estimación** | ≈ 4 h |
| **Día objetivo** | 1 |
| **Requisitos** | FR-019 |
| **Etiquetas** | `epic:EPIC-01` `area:infrastructure` `type:feature` `priority:P0` |
| **Estado** | Pendiente |
| **Issue de GitHub** | [#3](https://github.com/djacobo-hiberuscom/taskflow-onboarding/issues/3) |
| **Rama sugerida** | `feature/ISS-01-apphost-db` |

## Contexto

Es el punto de arranque de todo el entorno y base de la persistencia.

## Alcance

- [ ] Crear `infrastructure/TaskFlow.AppHost` (.NET 10) con PostgreSQL 18, volumen persistente y bases `taskflow` y `keycloak`.
- [ ] Script de inicialización con roles `taskflow_app`, `taskflow_migrator` y `keycloak`; secretos con user-secrets.
- [ ] Registrar la API en `http://localhost:5080` con su configuración inyectada y CORS a `http://localhost:5173`.
- [ ] Crear `global.json`, `.gitignore` y las carpetas `backend/`, `frontend/`, `infrastructure/`.

## Criterios de aceptación

- [ ] `aspire run` arranca PostgreSQL y los datos persisten tras reiniciar.
- [ ] La cuenta de la API no puede ejecutar DDL.
- [ ] Sin secretos en Git.

## Dependencias

**Bloqueada por:** ninguna.

**Bloquea a:** [ISS-02](ISS-02-backend-base.md), [ISS-03](ISS-03-keycloak-realm.md), [ISS-04](ISS-04-spa-base.md)

## Verificación

Arrancar, insertar un dato y reiniciar.

## Referencias

- [04-development/environment-setup.md](../04-development/environment-setup.md)
- [03-architecture/backend.md](../03-architecture/backend.md)

Aplica la [definición de hecho](README.md#definición-de-hecho).
