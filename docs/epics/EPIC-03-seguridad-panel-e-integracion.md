# EPIC-03 — Seguridad, panel e integración final

| Campo | Valor |
|---|---|
| **Calendario** | Días 2–5 |
| **Etiqueta** | `epic:EPIC-03` |
| **Estado** | Pendiente |
| **Carga** | David 8 h · Oliver 10 h · conjunta 3 h |

## Objetivo

Proteger la API con Keycloak, integrar la sesión en la SPA, añadir el panel y cerrar con la demostración de los recorridos.

## Alcance

- JWT, roles, usuario local, introspección, panel.
- OIDC con PKCE, sesión, rutas y controles por rol, panel, perfil.
- Integración final y demostración local.

**Fuera de alcance**

- Despliegue en AWS (FR-020) y CI: quedan fuera de esta semana y se mantienen como están documentados en el roadmap (fase 5).

## Criterio de cierre

Recorridos A, B, C y E funcionan con Keycloak y la API reales; las respuestas 401/403/404 son coherentes; no quedan defectos bloqueantes.

## Issues

| ID | Título | Resp. | Prio. | Horas | Día | Bloqueada por |
|---|---|---|:---:|:---:|:---:|---|
| [ISS-12](../issues/ISS-12-jwt-auth.md) | JWT de Keycloak, roles y usuario local (GET /me) | David | P0 | 4 | 3 | ISS-02, ISS-03 |
| [ISS-13](../issues/ISS-13-introspection.md) | Introspección por petición y 503 ante fallo del proveedor | David | P2 | 2 | 5 | ISS-12 |
| [ISS-14](../issues/ISS-14-dashboard-api.md) | Endpoint del panel de indicadores | David | P1 | 2 | 5 | ISS-06, ISS-07 |
| [ISS-15](../issues/ISS-15-oidc-session.md) | OIDC con PKCE, sesión, rutas protegidas y controles por rol | Oliver | P0 | 6 | 2 | ISS-03, ISS-04 |
| [ISS-16](../issues/ISS-16-dashboard-profile-ui.md) | Panel de indicadores y Perfil | Oliver | P1 | 4 | 5 | ISS-15, ISS-14 |
| [ISS-17](../issues/ISS-17-integration-demo.md) | Integración final y demostración de los recorridos A, B, C y E | Ambos | P0 | 3 | 5 | ISS-07, ISS-08, ISS-11, ISS-16, ISS-09, ISS-10 |

