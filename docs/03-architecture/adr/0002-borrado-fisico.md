# ADR-0002 — Borrado físico de tareas y proyectos

> **Estado:** Propuesto
>
> **Fecha:** 2026-10-22
>
> **Autores:** David (backend), equipo de desarrollo
>
> **Aprobación:** pendiente del responsable técnico
>
> **Requisitos relacionados:** FR-012, FR-021, FR-017 · BR-014 · requisitos §8, cuestión 4

## Contexto

Los requisitos exigen eliminar tareas (cualquier miembro de un proyecto activo, o `Admin`) y proyectos (solo `Admin`), y que lo eliminado desaparezca de listas, «Mis tareas» y panel. La forma de borrar (física o lógica) seguía abierta. Para conservar un proyecto sin mostrarlo ya existe el archivado, que es no destructivo y reversible.

## Alternativas consideradas

1. **Borrado lógico** (`deleted_at`). Permite recuperar y auditar, pero obliga a filtrar en todas las consultas, complica unicidades e índices y no hay requisito de papelera ni de auditoría en el MVP.
2. **Borrado físico** con integridad referencial en la base de datos. Más sencillo y sin riesgo de que una consulta olvide el filtro; lo eliminado no se recupera.

## Decisión

- Las tareas se eliminan **físicamente** (`DELETE`), sin columna `deleted_at`.
- Los proyectos se eliminan físicamente; la base de datos elimina en cascada sus tareas y pertenencias (`ON DELETE CASCADE` desde `project`).
- La interfaz pide confirmación explícita y, para proyectos, recomienda archivar si se quiere conservar el historial.

## Consecuencias

- **Positivas:** consultas e indicadores más simples; FR-012, FR-017 y FR-021 se cumplen por construcción.
- **Negativas:** no hay recuperación de lo eliminado ni rastro de quién lo hizo; un error humano solo se corrige con la copia de seguridad.
- **Trabajo derivado:** reflejar en [database.md §4–5](../database.md); pruebas de cascada de proyecto con tareas asignadas (FK compuesta); actualizar la cuestión 4 de los requisitos cuando se acepte.
