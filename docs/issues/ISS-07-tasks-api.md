# ISS-07 — API de tareas: CRUD, estado y asignación atómica

| Campo | Valor |
|---|---|
| **Épica** | [EPIC-02](../epics/EPIC-02-mvp-proyectos-y-tareas.md) — MVP de proyectos y tareas |
| **Responsable** | David |
| **Revisión** | Oliver |
| **Prioridad** | P0 — imprescindible |
| **Estimación** | ≈ 7 h |
| **Día objetivo** | 5 |
| **Requisitos** | FR-009, FR-010, FR-011, FR-012, FR-013, FR-014, FR-023 |
| **Etiquetas** | `epic:EPIC-02` `area:backend` `type:feature` `priority:P0` |
| **Estado** | Pendiente |
| **Issue de GitHub** | #— |
| **Rama sugerida** | `feature/ISS-07-tasks-api` |

## Contexto

Gestión de tareas dentro de proyectos visibles, con alta de pertenencia al asignar.

## Alcance

- [ ] `POST /projects/{id}/tasks`, `GET/PUT/DELETE /tasks/{id}`, `PUT /tasks/{id}/status` y `PUT /tasks/{id}/assignee`, más `GET /users/assignable`.
- [ ] Alta de pertenencia en la misma transacción y tolerante a duplicados; la pertenencia se conserva al desasignar o eliminar.
- [ ] Estado por defecto `ToDo`, validaciones y 404 para tareas no visibles.

## Criterios de aceptación

- [ ] Asignar a un no miembro lo hace miembro y dos asignaciones simultáneas no duplican.
- [ ] Una tarea no cambia de proyecto.
- [ ] Pruebas de integración de los recorridos A y B.

## Dependencias

**Bloqueada por:** [ISS-06](ISS-06-projects-api.md) (API de proyectos con visibilidad, pertenencia y bloqueo por proyecto)

**Bloquea a:** [ISS-08](ISS-08-task-queries.md), [ISS-14](ISS-14-dashboard-api.md), [ISS-17](ISS-17-integration-demo.md)

## Verificación

`dotnet test`.

## Referencias

- [05-api/api-contracts.md](../05-api/api-contracts.md)
- [03-architecture/backend.md](../03-architecture/backend.md)

Aplica la [definición de hecho](README.md#definición-de-hecho).
