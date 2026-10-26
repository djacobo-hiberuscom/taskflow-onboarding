# EPIC-02 — MVP de proyectos y tareas

| Campo | Valor |
|---|---|
| **Calendario** | Días 2–5 |
| **Etiqueta** | `epic:EPIC-02` |
| **Estado** | Pendiente |
| **Carga** | David 23 h · Oliver 20 h · conjunta 0 h |

## Objetivo

Implementar proyectos, pertenencia, tareas, asignación, filtros y «Mis tareas» en API y SPA, con persistencia en PostgreSQL y visibilidad por pertenencia.

## Alcance

- Backend: dominio, modelo y migración, servicios con bloqueo por proyecto, endpoints de proyectos y tareas, pruebas de integración.
- Frontend: pantallas de proyectos y tareas, validación, confirmaciones y estados de carga, vacío y error.

**Fuera de alcance**

- Panel e identidad (EPIC-03).

## Criterio de cierre

Recorridos A, B y E de los requisitos completos contra la API real con PostgreSQL, y datos que sobreviven a un reinicio.

## Issues

| ID | Título | Resp. | Prio. | Horas | Día | Bloqueada por |
|---|---|---|:---:|:---:|:---:|---|
| [ISS-05](../issues/ISS-05-domain-model.md) | Dominio, modelo EF Core y migración inicial con pruebas del esquema | David | P0 | 5 | 2 | ISS-02 |
| [ISS-06](../issues/ISS-06-projects-api.md) | API de proyectos con visibilidad, pertenencia y bloqueo por proyecto | David | P0 | 7 | 4 | ISS-05, ISS-12 |
| [ISS-07](../issues/ISS-07-tasks-api.md) | API de tareas: CRUD, estado y asignación atómica | David | P0 | 7 | 5 | ISS-06 |
| [ISS-08](../issues/ISS-08-task-queries.md) | Listados de tareas, «Mis tareas» y semilla de demostración | David | P0 | 4 | 5 | ISS-07 |
| [ISS-09](../issues/ISS-09-projects-ui.md) | Pantallas de proyectos: lista, detalle, miembros y acciones de administración | Oliver | P0 | 6 | 3 | ISS-04 |
| [ISS-10](../issues/ISS-10-tasks-ui.md) | Pantallas de tareas: listado con filtros, detalle y formularios | Oliver | P0 | 8 | 4 | ISS-04 |
| [ISS-11](../issues/ISS-11-assign-status-ui.md) | Asignación, cambio de estado y «Mis tareas» | Oliver | P0 | 6 | 5 | ISS-10 |

