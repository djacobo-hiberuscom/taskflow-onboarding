# TaskFlow — Descripción del programa de incorporación

> **Programa:** incorporación técnica al desarrollo de aplicaciones web completas
>
> **Equipo:** 1 desarrollador de backend · 1 desarrollador de frontend · 1 responsable técnico
>
> **Duración:** 6 semanas, ajustables tras la evaluación inicial
>
> **Estado:** planificado; repositorio en fase de documentación
>
> **Repositorio:** `taskflow-onboarding`

## 1. Propósito

TaskFlow es una aplicación interna de gestión de proyectos y tareas que se desarrollará como ejercicio supervisado de incorporación técnica. El objetivo es demostrar que el equipo puede diseñar, implementar, documentar, probar, revisar, integrar y desplegar un sistema completo siguiendo prácticas profesionales de desarrollo.

El responsable técnico define el problema de negocio, las restricciones y los criterios de aceptación. Los desarrolladores proponen la solución técnica y la implementan de forma colaborativa. La arquitectura debe revisarse **antes de comenzar la implementación**.

## 2. Alcance del producto

El producto mínimo viable (MVP) permitirá:

- Consultar proyectos activos y sus tareas con un usuario autenticado y un rol admitido.
- Crear, editar y archivar proyectos con el rol `Admin`.
- Crear, asignar, editar y eliminar tareas con el rol `Admin`.
- Actualizar el estado de cualquier tarea activa como `Admin`, o de las tareas propias como `Member`.
- Filtrar tareas por proyecto, estado y prioridad, y ordenarlas por fecha de vencimiento o creación.
- Consultar las tareas asignadas al usuario actual.
- Ver un panel con el número de proyectos activos y tareas abiertas, completadas y vencidas.
- Aplicar los permisos en la API y reflejarlos en la interfaz.

### Roles

| Rol | Acciones permitidas |
|---|---|
| `Admin` | Gestionar proyectos, tareas y asignaciones; consultar todos los datos de los proyectos activos |
| `Member` | Consultar todos los proyectos activos y sus tareas; actualizar únicamente el estado de las tareas que tenga asignadas |

El MVP no incluye permisos de lectura específicos por proyecto. La consulta administrativa de proyectos archivados y sus tareas se concretará en la fase 0; su restauración es opcional. Los registros archivados se excluyen de las vistas activas y del panel.

Las dudas o propuestas de funcionalidad se registrarán como incidencias antes de implementarse. Quedan fuera del MVP: comentarios, notificaciones, colaboración en tiempo real, archivos adjuntos, facturación, gestión de múltiples organizaciones, aplicación móvil y gestión avanzada de miembros por proyecto.

## 3. Tecnologías previstas

| Área | Base tecnológica |
|---|---|
| API | ASP.NET Core, en una versión con soporte y aprobada por la empresa; C# |
| Acceso a datos | Entity Framework Core y migraciones |
| Base de datos | PostgreSQL |
| Identidad | Keycloak, OpenID Connect (OIDC) y tokens de acceso JWT |
| Interfaz | React + TypeScript + Vite |
| Bibliotecas de interfaz | TanStack Router, TanStack Query, Tailwind CSS y shadcn/ui |
| Infraestructura local | Docker y Docker Compose |
| Control de versiones y trabajo | GitHub, Issues, Projects y solicitudes de cambios (PR) |
| Pruebas | xUnit y pruebas de integración de ASP.NET Core; Vitest y React Testing Library |
| Entrega | Integración continua (CI) con GitHub Actions y despliegue en un entorno AWS aislado de producción |

Las versiones concretas se elegirán y documentarán en la fase 0. Cualquier sustitución requiere una justificación breve y la aprobación del responsable técnico. La solución debe evitar patrones o infraestructura distribuida que no aporten valor al ejercicio.

## 4. Responsabilidades del equipo

| Rol | Responsabilidades |
|---|---|
| Responsable técnico | Requisitos, prioridades, aprobación de arquitectura, revisión de código, acompañamiento, aprobación de seguridad y gasto en AWS, aceptación final |
| Desarrollador de backend | Modelo de datos, contratos de API, EF Core, autenticación y autorización, pruebas del backend y empaquetado de la API |
| Desarrollador de frontend | Recorridos de usuario, interfaz accesible, navegación, integración con la API, pruebas del frontend y empaquetado de la interfaz |
| Ambos desarrolladores | Propuesta de arquitectura, revisión de contratos, preparación del entorno, integración, documentación, automatización y explicación del despliegue |

Las interfaces entre componentes deben acordarse y verificarse conjuntamente.

## 5. Principios de entrega

1. **Incidencia previa:** cada cambio relevante se vincula a una incidencia de GitHub con criterios de aceptación.
2. **PR pequeñas:** una rama por incidencia, revisión antes de integrar y comprobaciones correctas en `main`.
3. **Contrato acordado:** definir solicitudes, respuestas y errores antes del trabajo paralelo de frontend y backend.
4. **Documentación como entregable:** registrar decisiones e instrucciones de entorno y despliegue en Git.
5. **Funcionamiento demostrable:** evaluar demostraciones y pruebas por encima de métricas de actividad.
6. **Seguridad desde el diseño:** excluir credenciales de Git, aplicar permisos en el servidor, usar HTTPS en AWS y limitar privilegios.
7. **Ciclo de vida completo:** el resultado final debe poder ejecutarse localmente y en AWS.

## 6. Dinámica de colaboración

| Actividad | Frecuencia | Resultado |
|---|---|---|
| Planificación | Lunes, 20–30 min | Incidencias semanales ordenadas, responsables y dependencias |
| Reunión diaria | Cada día laborable, 10 min | Avances, próximos pasos y bloqueos |
| Coordinación de contratos e integración | Cuando sea necesario, 15 min | Cambios de API y entregas acordadas |
| Revisión de PR | Durante la semana | Comentarios concretos y cambios aprobados |
| Demostración y retrospectiva | Viernes, 30–45 min | Demostración funcional, comentarios y acciones de mejora |

Si un bloqueo dura unos 45–60 minutos, se documentará lo intentado y se pedirá ayuda. El programa no requiere trabajar fuera de la jornada contratada.

## 7. Entregables principales

- Propuesta de arquitectura revisada y registros de decisiones arquitectónicas (ADR).
- Entorno local reproducible e instrucciones de preparación.
- API, base de datos, proveedor de identidad e interfaz en funcionamiento.
- MVP funcional con acceso por roles y pruebas automatizadas.
- Flujo de CI con comprobaciones reproducibles y artefactos de compilación.
- Despliegue en AWS fuera de producción, evidencias de verificación, guía de operación y retirada de recursos.
- Seguimiento de aprendizaje individual y demostración técnica final; las valoraciones personales se mantendrán en un espacio restringido.

Véanse los [objetivos](objectives.md), el [plan de trabajo](roadmap.md), el [seguimiento](evolution.md), los [requisitos funcionales](../02-requirements/functional-requirements.md) y la [arquitectura propuesta](../03-architecture/system-overview.md).
