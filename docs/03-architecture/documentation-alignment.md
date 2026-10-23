# TaskFlow — Coherencia documental y cambios pendientes

> **Responsables:** David y Oliver
>
> **Revisión:** 2026-10-23
>
> **Estado:** documentos del equipo sincronizados con los acuerdos del análisis; cambios en documentos del manager pendientes de su aprobación. No acredita implementación ni cierre formal de fase 0.

## 1. Motivo y alcance

El usuario ha indicado que se apliquen los acuerdos de coherencia únicamente a la documentación creada por el equipo. Los documentos entregados por el manager se mantienen intactos hasta obtener su aprobación:

- [system-overview.md](system-overview.md).
- [functional-requirements.md](../02-requirements/functional-requirements.md).
- [roadmap.md](../01-onboarding/roadmap.md).

Por ese motivo pueden seguir mostrando preguntas ya resueltas, nombres antiguos o una propuesta de entorno distinta. Las discrepancias se registran aquí para revisarlas y trasladarlas expresamente cuando se autorice. No se cambia silenciosamente la especificación original ni se presenta esta sincronización como aprobación del responsable técnico.

La elección de **Aspire para orquestar el desarrollo** procede de la última instrucción del usuario. Sustituye el arranque local antes descrito con Docker Compose; conserva Docker como motor de los contenedores, los puertos acordados y las migraciones explícitas por separado. Su guía está en [environment-setup.md](../04-development/environment-setup.md).

La **revocación en Keycloak con introspección en cada petición protegida** queda cerrada por indicación del usuario el 2026-10-23. Sustituye el pendiente anterior: token inactivo → 401 y fallo de comprobación → 503, sin ejecutar la operación. No implica aprobación del manager ni implementación; se mantienen los tiempos de sesión acordados.

## 2. Documentos del equipo sincronizados

| Documento | Contenido de referencia |
|---|---|
| [backend.md](backend.md) | Cuatro capas, Controllers, repositorios, validadores, perfil, permisos, transacciones y entorno con Aspire. |
| [database.md](database.md) | Modelo físico, UUID, `due_date`, claves, cascadas, último miembro y ciclo de vida del usuario local. |
| [api-contracts.md](../05-api/api-contracts.md) | DTO, límites, errores, paginación, ordenación, última escritura gana y OpenAPI generado desde Controllers. |
| [authentication.md](authentication.md) | Integración confirmada con frontend: realm, clientes, PKCE, tokens, sesión máxima de 10 horas, perfil y precedencia 404/403. Revocación en Keycloak con introspección por petición. |
| [frontend.md](frontend.md) | Consumo del contrato, sesión, validadores, reintentos de lecturas y tratamiento de errores. |
| [CONTRIBUTING.md](../../CONTRIBUTING.md) | Coordinación de cambios, documentación y protección temporal de los originales del manager. |
| [environment-setup.md](../04-development/environment-setup.md) | Diseño del entorno con Aspire y pasos que se implementarán y verificarán en fase 1. |

Se conserva una fuente de detalle por tema y se enlaza desde los demás documentos para evitar duplicar instrucciones completas.

## 3. Traslado pendiente a los documentos del manager

| Archivo y apartado | Cambios pendientes de revisión y aprobación |
|---|---|
| `system-overview.md` §3 y §11 | Actualizar la ubicación real de backend, la existencia de CONTRIBUTING y el estado de las decisiones ya acordadas. Mantener la revisión formal de arquitectura identificada. |
| `system-overview.md` §4–5 | Reflejar acuerdos de identidad, perfil y tiempos de sesión; priorizar 404 para recursos concretos no visibles antes de exigir la operación administrativa. Incorporar la decisión de revocación en Keycloak con introspección por petición, 401 para token inactivo y 503 si no puede comprobarse. |
| `system-overview.md` §6 | Alinear el diagrama y las referencias con UUID, `due_date`, PK de pertenencia `(project_id, user_id)`, mínimo de un miembro y procedimiento de borrado local. Sustituir la concurrencia de edición abierta por última escritura gana y enlazar el mecanismo de bloqueos. |
| `system-overview.md` §7–8 | Reflejar contrato, paginación y límites acordados, y el uso de Aspire AppHost para el desarrollo. Mantener API/Vite como procesos del host y dependencias en contenedores. |
| `system-overview.md` §9 | Señalar el aplazamiento de la propuesta AWS para que no se interprete como una opción elegida o ya redactada. |
| `functional-requirements.md` FR-009 | Aclarar que toda tarea persiste un estado obligatorio y que omitirlo en el cuerpo de creación aplica `ToDo`; enviar `null` es inválido. |
| `functional-requirements.md` matriz, criterios y §8 | Alinear los casos 404/403 y actualizar las preguntas resueltas durante el análisis, incorporando la revocación acordada con introspección y conservando como pendiente la revisión del responsable técnico. |
| `roadmap.md` fase 0 | Corregir el inventario de entregables: backend está en arquitectura y CONTRIBUTING existe. Conciliar el aplazamiento de AWS con el criterio original de cierre y el registro progresivo de ADR. |
| `roadmap.md` fase 1 | Reflejar Aspire como orquestador local, sus prerequisitos y el AppHost. Mantener actualización explícita del esquema y guía de entorno verificable. |

Este listado registra cambios pendientes; los tres archivos anteriores no se editan en esta actualización.

## 4. Trabajo que permanece pendiente

- Implementar y comprobar la revocación acordada: cliente confidencial de API, secreto seguro, introspección por petición, rechazo del mismo token tras logout y tratamiento de 401/503.
- Obtener aprobación para trasladar los acuerdos a sus documentos originales y revisar los criterios de cierre de fase 0.
- Implementar y comprobar AppHost, endpoints, configuración, realm, roles de PostgreSQL, migraciones y datos de demostración.
- Fijar versiones estables compatibles de Aspire e integraciones en los archivos de implementación.
- Generar OpenAPI y ejecutar compilación y pruebas cuando existan los proyectos.
- Completar ADR conforme avance el trabajo; preparar el despliegue cuando se aborde esa fase.

Al autorizarse la actualización de los originales, se revisará esta tabla y se registrará qué cambios se han trasladado y cuáles siguen abiertos. Hasta entonces, los acuerdos técnicos y sus límites se consultan en los documentos del equipo enlazados en §2.
