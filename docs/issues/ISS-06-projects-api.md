# ISS-06 — API de proyectos con visibilidad, pertenencia y bloqueo por proyecto

| Campo | Valor |
|---|---|
| **Épica** | [EPIC-02](../epics/EPIC-02-mvp-proyectos-y-tareas.md) — MVP de proyectos y tareas |
| **Responsable** | David |
| **Revisión** | Oliver |
| **Prioridad** | P0 — imprescindible |
| **Estimación** | ≈ 7 h |
| **Día objetivo** | 4 |
| **Requisitos** | FR-004, FR-005, FR-006, FR-007, FR-008, FR-021, FR-022, FR-023 |
| **Etiquetas** | `epic:EPIC-02` `area:backend` `type:feature` `priority:P0` |
| **Estado** | Pendiente |
| **Issue de GitHub** | #— |
| **Rama sugerida** | `feature/ISS-06-projects-api` |

## Contexto

Reúne repositorios, unidad de trabajo y el protocolo `SELECT … FOR UPDATE` que usarán también las tareas.

## Alcance

- [ ] `ProjectService`, repositorios, `IUnitOfWork` y bloqueo de la fila del proyecto en toda escritura.
- [ ] `GET/POST /projects`, `GET/PUT /projects/{id}`, `archive`, `restore`, `DELETE` y `?archived=true`, con paginación.
- [ ] Visibilidad filtrada antes de contar y paginar; matriz 403/404 del contrato; creador y pertenencia en una sola transacción.

## Criterios de aceptación

- [ ] `User` solo ve proyectos activos de los que es miembro y el resto responde 404.
- [ ] Archivar conserva datos y eliminar borra en cascada; `User` recibe 403.
- [ ] Pruebas de integración de los recorridos A y E.

## Dependencias

**Bloqueada por:** [ISS-05](ISS-05-domain-model.md) (Dominio, modelo EF Core y migración inicial con pruebas del esquema), [ISS-12](ISS-12-jwt-auth.md) (JWT de Keycloak, roles y usuario local (GET /me))

**Bloquea a:** [ISS-07](ISS-07-tasks-api.md), [ISS-14](ISS-14-dashboard-api.md)

## Verificación

`dotnet test` con usuarios A, B, C y Admin.

## Referencias

- [05-api/api-contracts.md](../05-api/api-contracts.md)
- [03-architecture/backend.md](../03-architecture/backend.md)
- [03-architecture/authentication.md](../03-architecture/authentication.md)

Aplica la [definición de hecho](README.md#definición-de-hecho).
