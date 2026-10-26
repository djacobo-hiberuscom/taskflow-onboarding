# ISS-16 — Panel de indicadores y Perfil

| Campo | Valor |
|---|---|
| **Épica** | [EPIC-03](../epics/EPIC-03-seguridad-panel-e-integracion.md) — Seguridad, panel e integración final |
| **Responsable** | Oliver |
| **Revisión** | David |
| **Prioridad** | P1 — MVP |
| **Estimación** | ≈ 4 h |
| **Día objetivo** | 5 |
| **Requisitos** | FR-017, FR-003, FR-002 |
| **Etiquetas** | `epic:EPIC-03` `area:frontend` `type:feature` `priority:P1` |
| **Estado** | Pendiente |
| **Issue de GitHub** | #— |
| **Rama sugerida** | `feature/ISS-16-dashboard-profile-ui` |

## Contexto

Muestra lo que cada usuario puede ver y permite cerrar sesión.

## Alcance

- [ ] Panel con proyectos activos y tareas abiertas, completadas y vencidas, invalidado tras mutaciones.
- [ ] Perfil con nombre, correo y rol y botón de cierre de sesión.

## Criterios de aceptación

- [ ] Los valores coinciden con la API y se actualizan tras cambios.
- [ ] Estados de carga y error visibles.
- [ ] El cierre de sesión finaliza la de Keycloak.

## Dependencias

**Bloqueada por:** [ISS-15](ISS-15-oidc-session.md) (OIDC con PKCE, sesión, rutas protegidas y controles por rol), [ISS-14](ISS-14-dashboard-api.md) (Endpoint del panel de indicadores)

**Bloquea a:** [ISS-17](ISS-17-integration-demo.md)

## Verificación

Recorrido manual B.

## Referencias

- [03-architecture/frontend.md](../03-architecture/frontend.md)

Aplica la [definición de hecho](README.md#definición-de-hecho).
