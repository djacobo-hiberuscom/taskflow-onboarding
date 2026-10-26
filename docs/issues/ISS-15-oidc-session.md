# ISS-15 — OIDC con PKCE, sesión, rutas protegidas y controles por rol

| Campo | Valor |
|---|---|
| **Épica** | [EPIC-03](../epics/EPIC-03-seguridad-panel-e-integracion.md) — Seguridad, panel e integración final |
| **Responsable** | Oliver |
| **Revisión** | David |
| **Prioridad** | P0 — imprescindible |
| **Estimación** | ≈ 6 h |
| **Día objetivo** | 2 |
| **Requisitos** | FR-001, FR-002, FR-003 |
| **Etiquetas** | `epic:EPIC-03` `area:frontend` `type:feature` `priority:P0` |
| **Estado** | Pendiente |
| **Issue de GitHub** | #— |
| **Rama sugerida** | `feature/ISS-15-oidc-session` |

## Contexto

TaskFlow no gestiona contraseñas: la SPA redirige a Keycloak.

## Alcance

- [ ] `keycloak-js` en `check-sso` con PKCE `S256`; InitPage, LoginPage, RegisterPage y Callback con ruta guardada.
- [ ] Tokens solo en memoria, renovación con menos de 30 s, bearer en el cliente, 401 → Timeout, 503 como indisponibilidad; logout limpia la caché.
- [ ] `beforeLoad` de sesión, guarda por rol y controles de `Admin` ocultos a `User` según `GET /me`.

## Criterios de aceptación

- [ ] Una ruta protegida sin sesión va al login y vuelve a ella.
- [ ] No se guarda ningún token en el almacenamiento del navegador.
- [ ] `User` no ve archivar, eliminar ni Archivados.

## Dependencias

**Bloqueada por:** [ISS-03](ISS-03-keycloak-realm.md) (Keycloak en AppHost: realm, clientes y usuarios de demostración), [ISS-04](ISS-04-spa-base.md) (SPA base: Vite, TanStack, shadcn, navegación, cliente tipado y simulaciones)

**Bloquea a:** [ISS-16](ISS-16-dashboard-profile-ui.md)

## Verificación

Recorrido manual con los usuarios de demostración.

## Referencias

- [03-architecture/frontend.md](../03-architecture/frontend.md)
- [03-architecture/authentication.md](../03-architecture/authentication.md)

Aplica la [definición de hecho](README.md#definición-de-hecho).
