# ISS-03 — Keycloak en AppHost: realm, clientes y usuarios de demostración


| Campo | Valor |
|---|---|
| **Épica** | [EPIC-01](../epics/EPIC-01-entorno-y-bases.md) — Entorno Aspire y bases técnicas |
| **Responsable** | Oliver |
| **Revisión** | David |
| **Prioridad** | P0 — imprescindible |
| **Estimación** | ≈ 4 h |
| **Día objetivo** | 1 |
| **Requisitos** | FR-001, FR-003 |
| **Etiquetas** | `epic:EPIC-01` `area:identity` `type:feature` `priority:P0` |
| **Estado** | Pendiente |
| **Issue de GitHub** | [#5](https://github.com/djacobo-hiberuscom/taskflow-onboarding/issues/5) |
| **Rama sugerida** | `feature/ISS-03-keycloak-realm` |

## Contexto

La API y la SPA necesitan un realm reproducible desde el primer día.

## Alcance

- [ ] Contenedor de Keycloak en `localhost:8080` con su base en PostgreSQL e importación del realm `taskflow`.
- [ ] Clientes `taskflow-spa` (público, PKCE `S256`) y `taskflow-api` (confidencial, introspección, audiencia `taskflow-api`); roles `Admin` y `User` (por defecto), registro habilitado.
- [ ] Tokens de 5 min, sesión SSO 30 min / 10 h; usuarios Admin, A, B y C con `sub` fijo.
- [ ] Entregar a David la tabla `sub` → rol; el secreto del cliente fuera del JSON versionado.

## Criterios de aceptación

- [ ] Se obtiene un token con PKCE para cada usuario, con audiencia y roles.
- [ ] El `sub` no cambia al reimportar.
- [ ] Registrarse da rol `User`, nunca `Admin`.

## Dependencias

**Bloqueada por:** [ISS-01](ISS-01-apphost-db.md) (AppHost de Aspire con PostgreSQL 18 y roles separados)

**Bloquea a:** [ISS-12](ISS-12-jwt-auth.md), [ISS-15](ISS-15-oidc-session.md)

## Verificación

Obtener y decodificar un token por usuario.

## Referencias

- [03-architecture/authentication.md](../03-architecture/authentication.md)
- [04-development/environment-setup.md](../04-development/environment-setup.md)

Aplica la [definición de hecho](README.md#definición-de-hecho).
