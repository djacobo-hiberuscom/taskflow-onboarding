# EPIC-01 — Entorno Aspire y bases técnicas

| Campo | Valor |
|---|---|
| **Calendario** | Días 1–2 |
| **Etiqueta** | `epic:EPIC-01` |
| **Estado** | Pendiente |
| **Carga** | David 10 h · Oliver 10 h · conjunta 0 h |

## Objetivo

Tener `aspire run` levantando PostgreSQL, Keycloak, API y SPA, con los esqueletos de backend y frontend sobre los que trabajar en paralelo.

## Alcance

- AppHost, PostgreSQL con roles separados, Keycloak con realm y usuarios de demostración, Vite.
- Solución backend de cuatro capas con API base, EF Core y fixture de pruebas.
- SPA con navegación, cliente tipado y simulaciones del contrato.

**Fuera de alcance**

- Lógica de negocio.
- Documentación: la existente es la referencia y no se modifica.

## Criterio de cierre

Un clon limpio arranca todo con `aspire run`, `/health` responde, la SPA se muestra y un usuario de demostración inicia sesión en Keycloak.

## Issues

| ID | Título | Resp. | Prio. | Horas | Día | Bloqueada por |
|---|---|---|:---:|:---:|:---:|---|
| [ISS-01](../issues/ISS-01-apphost-db.md) | AppHost de Aspire con PostgreSQL 18 y roles separados | David | P0 | 4 | 1 | — |
| [ISS-02](../issues/ISS-02-backend-base.md) | Solución backend de cuatro capas, API base, EF Core y pruebas | David | P0 | 6 | 2 | ISS-01 |
| [ISS-03](../issues/ISS-03-keycloak-realm.md) | Keycloak en AppHost: realm, clientes y usuarios de demostración | Oliver | P0 | 4 | 1 | ISS-01 |
| [ISS-04](../issues/ISS-04-spa-base.md) | SPA base: Vite, TanStack, shadcn, navegación, cliente tipado y simulaciones | Oliver | P0 | 6 | 2 | ISS-01 |

