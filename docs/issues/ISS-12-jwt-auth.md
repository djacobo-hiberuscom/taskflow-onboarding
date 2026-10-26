# ISS-12 — JWT de Keycloak, roles y usuario local (GET /me)

| Campo | Valor |
|---|---|
| **Épica** | [EPIC-03](../epics/EPIC-03-seguridad-panel-e-integracion.md) — Seguridad, panel e integración final |
| **Responsable** | David |
| **Revisión** | Oliver |
| **Prioridad** | P0 — imprescindible |
| **Estimación** | ≈ 4 h |
| **Día objetivo** | 3 |
| **Requisitos** | FR-001, FR-003 |
| **Etiquetas** | `epic:EPIC-03` `area:backend` `type:feature` `priority:P0` |
| **Estado** | Pendiente |
| **Issue de GitHub** | #— |
| **Rama sugerida** | `feature/ISS-12-jwt-auth` |

## Contexto

La API acepta solo access tokens válidos y resuelve el `AppUser` por `sub`.

## Alcance

- [ ] JWT Bearer con firma, emisor, audiencia y vigencia; transformación de `realm_access.roles` a `Admin`/`User` y políticas.
- [ ] `ICurrentUser` real; alta del `AppUser` en la primera petición sin duplicados; `GET /me`.
- [ ] 401 sin token válido y 403 sin rol admitido.

## Criterios de aceptación

- [ ] Token inválido, caducado o de otra audiencia devuelve 401.
- [ ] Token sin rol admitido devuelve 403.
- [ ] Dos primeras peticiones simultáneas crean un único `AppUser`.

## Dependencias

**Bloqueada por:** [ISS-02](ISS-02-backend-base.md) (Solución backend de cuatro capas, API base, EF Core y pruebas), [ISS-03](ISS-03-keycloak-realm.md) (Keycloak en AppHost: realm, clientes y usuarios de demostración)

**Bloquea a:** [ISS-06](ISS-06-projects-api.md), [ISS-13](ISS-13-introspection.md)

## Verificación

Pruebas de integración con tokens firmados de prueba.

## Referencias

- [03-architecture/authentication.md](../03-architecture/authentication.md)
- [03-architecture/backend.md](../03-architecture/backend.md)

Aplica la [definición de hecho](README.md#definición-de-hecho).
