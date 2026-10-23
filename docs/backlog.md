# TaskFlow — Mejoras futuras

Este documento reúne las mejoras que se consideran para después del MVP. Una entrada es una idea pendiente de priorizar, no un requisito ni un compromiso de implementación. Los requisitos actuales siguen en [functional-requirements.md](02-requirements/functional-requirements.md).

## Índice

| ID | Mejora | Estado | Prioridad |
|---|---|---|---|
| FUT-001 | Detectar conflictos entre ediciones simultáneas. | Diferida; fuera del MVP. | Por decidir. |

## FUT-001 — Detección de conflictos de edición

- **Origen:** decisión de backend del 2026-10-23: última escritura gana para el MVP.
- **Problema:** una persona puede guardar cambios basados en una versión antigua y sobrescribir los de otra sin recibir aviso.
- **Idea:** incorporar una versión de recurso y una comprobación de concurrencia optimista. Si se usa `ETag`/`If-Match`, respetar su semántica HTTP; la alternativa con versión en el cuerpo deberá definirse en el contrato.
- **Impacto:** persistencia/EF Core, DTO y contrato HTTP, interfaz para informar del conflicto y recargar, y pruebas.
- **Siguiente paso:** priorizar después del MVP y comparar alternativas antes de implementarlo.

## Cómo añadir una mejora

Asignar un identificador `FUT-###` e indicar problema, idea, origen, impacto, estado y prioridad. Si se crea una incidencia de GitHub, enlazarla desde aquí y utilizarla para seguir el trabajo, evitando mantener dos estados distintos. Una mejora aceptada para el producto se trasladará a requisitos y a un ADR cuando corresponda.
