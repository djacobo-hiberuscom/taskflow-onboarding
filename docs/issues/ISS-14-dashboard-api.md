# ISS-14 — Endpoint del panel de indicadores

| Campo | Valor |
|---|---|
| **Épica** | [EPIC-03](../epics/EPIC-03-seguridad-panel-e-integracion.md) — Seguridad, panel e integración final |
| **Responsable** | David |
| **Revisión** | Oliver |
| **Prioridad** | P1 — MVP |
| **Estimación** | ≈ 2 h |
| **Día objetivo** | 5 |
| **Requisitos** | FR-017 |
| **Etiquetas** | `epic:EPIC-03` `area:backend` `type:feature` `priority:P1` |
| **Estado** | Pendiente |
| **Issue de GitHub** | #— |
| **Rama sugerida** | `feature/ISS-14-dashboard-api` |

## Contexto

Recuentos en servidor con la visibilidad del usuario y la fecha de negocio.

## Alcance

- [ ] `GET /dashboard` con proyectos activos y tareas abiertas, completadas y vencidas.
- [ ] Fecha de hoy en `Europe/Madrid`; excluye archivados y eliminados.

## Criterios de aceptación

- [ ] Vencida es fecha anterior a hoy y no completada; sin fecha no vence.
- [ ] Global para `Admin` y acotado para `User`.
- [ ] Refleja los cambios tras mutaciones.

## Dependencias

**Bloqueada por:** [ISS-06](ISS-06-projects-api.md) (API de proyectos con visibilidad, pertenencia y bloqueo por proyecto), [ISS-07](ISS-07-tasks-api.md) (API de tareas: CRUD, estado y asignación atómica)

**Bloquea a:** [ISS-16](ISS-16-dashboard-profile-ui.md)

## Verificación

Pruebas con ayer, hoy, sin fecha y completada.

## Referencias

- [05-api/api-contracts.md](../05-api/api-contracts.md)
- [03-architecture/database.md](../03-architecture/database.md)

Aplica la [definición de hecho](README.md#definición-de-hecho).
