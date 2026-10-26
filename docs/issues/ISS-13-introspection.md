# ISS-13 — Introspección por petición y 503 ante fallo del proveedor

| Campo | Valor |
|---|---|
| **Épica** | [EPIC-03](../epics/EPIC-03-seguridad-panel-e-integracion.md) — Seguridad, panel e integración final |
| **Responsable** | David |
| **Revisión** | Oliver |
| **Prioridad** | P2 — se aplaza si no hay tiempo |
| **Estimación** | ≈ 2 h |
| **Día objetivo** | 5 |
| **Requisitos** | FR-002 |
| **Etiquetas** | `epic:EPIC-03` `area:backend` `type:feature` `priority:P2` |
| **Estado** | Pendiente |
| **Issue de GitHub** | #— |
| **Rama sugerida** | `feature/ISS-13-introspection` |

## Contexto

Garantiza que logout y revocación invalidan el token ya emitido. Es lo primero que se aplaza si no hay tiempo, a coste de dejar FR-002 con validación solo local.

## Alcance

- [ ] Introspección en cada petición protegida con el cliente `taskflow-api` y su secreto solo en la API.
- [ ] Inactivo → 401; proveedor caído o respuesta inválida → 503 sin ejecutar escrituras; `/health` exento.

## Criterios de aceptación

- [ ] El mismo token funciona antes del logout y devuelve 401 después.
- [ ] Con Keycloak detenido se responde 503 sin escribir.
- [ ] El secreto no aparece en Git ni en logs.

## Dependencias

**Bloqueada por:** [ISS-12](ISS-12-jwt-auth.md) (JWT de Keycloak, roles y usuario local (GET /me))

**Bloquea a:** ninguna.

## Verificación

Prueba con introspección simulada y comprobación manual.

## Referencias

- [03-architecture/authentication.md](../03-architecture/authentication.md)
- [04-development/environment-setup.md](../04-development/environment-setup.md)

Aplica la [definición de hecho](README.md#definición-de-hecho).
