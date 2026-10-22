# TaskFlow — Registro de decisiones arquitectónicas (ADR)

> **Responsable:** conjunto — ambos desarrolladores; aprobación del responsable técnico
>
> **Estado:** fase 0; todos los ADR están en estado «Propuesto» hasta su aprobación

## 1. Qué es un ADR

Un ADR registra una decisión arquitectónica relevante: su contexto, las alternativas consideradas, la decisión y sus consecuencias. Sirve para que cualquier persona entienda **por qué** el sistema es como es sin reconstruir la conversación original.

Se escribe un ADR cuando una decisión:

- Afecta a varios componentes o a ambos desarrolladores.
- Es costosa de revertir (modelo de datos, autenticación, despliegue).
- Resuelve una cuestión abierta de la fase 0 ([requisitos §8](../../02-requirements/functional-requirements.md) o [visión general §11](../system-overview.md)).

## 2. Convenciones

- Un archivo por decisión: `NNNN-titulo-en-kebab-case.md`, numeración correlativa que no se reutiliza.
- **Estados:** `Propuesto` → `Aceptado` o `Rechazado`; más adelante, `Sustituido por ADR-NNNN` u `Obsoleto`.
- Un ADR aceptado no se reescribe: si la decisión cambia, se crea uno nuevo que lo sustituye y se actualiza el estado del anterior.
- La aprobación la da el responsable técnico en la PR que cambia el estado a `Aceptado`.
- Al aceptar un ADR se actualizan en la misma PR la [visión general del sistema](../system-overview.md), los requisitos si afectan al producto y [CLAUDE.md](../../../CLAUDE.md).

## 3. Índice

| ADR | Título | Estado | Fecha |
|---|---|---|---|
| [0001](0001-pertenencia-a-proyectos.md) | Visibilidad de proyectos por pertenencia (`AppUser_Project`) | Propuesto | 2026-10-22 |
| [0002](0002-borrado-fisico.md) | Borrado físico de tareas y proyectos | Propuesto | 2026-10-22 |

Decisiones candidatas a ADR pendientes de redactar: versiones del stack y distribución de proyectos del backend, mapeo de roles de Keycloak y vigencia de tokens ([authentication.md](../authentication.md)), opción de despliegue AWS ([deployment-aws.md](../deployment-aws.md)), formato de errores y representación de estados en la API ([api-contracts.md](../../05-api/api-contracts.md)), zona horaria de negocio.

## 4. Plantilla

```markdown
# ADR-NNNN — Título breve de la decisión

> **Estado:** Propuesto | Aceptado | Rechazado | Sustituido por ADR-NNNN | Obsoleto
>
> **Fecha:** AAAA-MM-DD
>
> **Autores:** …
>
> **Aprobación:** responsable técnico, PR #…
>
> **Requisitos relacionados:** FR-…, BR-…

## Contexto

Qué problema o fuerza obliga a decidir. Restricciones, requisitos afectados y supuestos.

## Alternativas consideradas

1. **Alternativa A** — descripción breve. Ventajas / inconvenientes.
2. **Alternativa B** — …

## Decisión

Qué se decide, formulado de forma verificable.

## Consecuencias

- Positivas: …
- Negativas o costes asumidos: …
- Trabajo derivado (incidencias, documentos a actualizar, pruebas): …
```
