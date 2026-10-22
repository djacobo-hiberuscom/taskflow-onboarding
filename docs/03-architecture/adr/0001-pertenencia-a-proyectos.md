# ADR-0001 — Visibilidad de proyectos por pertenencia (`AppUser_Project`)

> **Estado:** Propuesto
>
> **Fecha:** 2026-10-22
>
> **Autores:** equipo de desarrollo
>
> **Aprobación:** pendiente del responsable técnico
>
> **Requisitos relacionados:** FR-004, FR-005, FR-013, FR-023 · BR-011, BR-012, BR-013, BR-015

## Contexto

La especificación inicial tenía dos roles, `Admin` y `Member`, y ambos podían leer todos los proyectos activos; `Member` solo podía cambiar el estado de sus tareas. La revisión del 2026-10-22 cambia el producto:

- El rol no administrativo pasa a llamarse `User` y puede crear proyectos y gestionar por completo sus tareas.
- `User` solo debe ver los proyectos que ha creado o en los que tiene tareas asignadas, y nunca los archivados.
- `Admin` ve y gestiona todo.

Hace falta representar «qué proyectos ve cada usuario» de forma consultable, coherente y difícil de saltarse.

## Alternativas consideradas

1. **Derivar la visibilidad en cada consulta** (`created_by = U` o existe una tarea con `assigned_id = U`). Sin tabla nueva, pero la regla se repite en todas las consultas, es más costosa y el usuario dejaría de ver el proyecto en cuanto le quitaran su última tarea.
2. **Tabla de pertenencia `AppUser_Project` alimentada automáticamente** al crear el proyecto y al asignar una tarea. La visibilidad es una simple unión; la asignación se puede restringir a miembros con una clave foránea compuesta.
3. **Gestión manual de miembros** (invitar o expulsar). Más flexible, pero añade pantallas y reglas fuera del alcance del MVP.

## Decisión

Se adopta la alternativa 2:

- `AppUser` N:M `Project` mediante `AppUser_Project` (clave primaria compuesta `user_id` + `project_id`).
- El servidor da de alta la pertenencia del creador al crear el proyecto y la del asignado al asignar una tarea a un no miembro, en la misma transacción e idempotentemente.
- La asignación se modela como `AppUser_Project` 1:N `Task`: `Task(project_id, assigned_id)` es una clave foránea compuesta hacia `AppUser_Project(project_id, user_id)`.
- `User` ve un proyecto si es miembro y `archived_at` es nulo; `Admin` no aplica el filtro de pertenencia.
- Los recursos no visibles responden HTTP 404 (pendiente de confirmar frente a 403).
- En el MVP, desasignar no retira la pertenencia.

## Consecuencias

- **Positivas:** la visibilidad es una unión indexada; la base de datos impide asignar a no miembros; la regla está en un único sitio (casos de uso que parten de la pertenencia).
- **Negativas:** una tabla y una FK compuesta más; un usuario que fue asignado y luego desasignado sigue viendo el proyecto (cuestión 10 de los requisitos); la FK compuesta requiere `ON DELETE SET NULL (assigned_id)`, disponible en PostgreSQL 15 o posterior, o una alternativa en el caso de uso.
- **Trabajo derivado:** modelo en [database.md](../database.md), autorización en [authentication.md](../authentication.md), contrato en [api-contracts.md](../../05-api/api-contracts.md), pruebas de visibilidad en todas las vías (detalle, listas, filtros, panel).
