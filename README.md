# TaskFlow

**Gestión de proyectos y tareas como punto de partida para una incorporación técnica práctica.**

TaskFlow es un proyecto de desarrollo de una aplicación web para organizar proyectos, asignar tareas y consultar el progreso del trabajo. Combina un backend con ASP.NET Core y una interfaz con React, con PostgreSQL para la persistencia y Keycloak para la autenticación y los roles.

El proyecto forma parte de un programa de incorporación técnica en el que un desarrollador de backend y otro de frontend trabajan con el acompañamiento de un responsable técnico. El recorrido abarca desde el análisis de requisitos y el diseño de la arquitectura hasta las pruebas, la revisión de código y el despliegue en AWS.

## Propósito

Desarrollar TaskFlow permitirá practicar el ciclo completo de entrega de software:

- Convertir necesidades de negocio en requisitos y criterios de aceptación verificables.
- Diseñar una arquitectura y acordar contratos entre frontend y backend.
- Implementar reglas de negocio, persistencia y permisos de acceso.
- Colaborar mediante incidencias, ramas y solicitudes de cambios (PR) en GitHub.
- Verificar el comportamiento con pruebas y automatizar las comprobaciones de calidad.
- Desplegar la aplicación y documentar su configuración y operación.

## Funcionalidades previstas

El producto mínimo viable (MVP) incluirá:

- **Proyectos:** creación, consulta, edición y archivado sin eliminar sus datos.
- **Tareas:** creación, consulta, edición, eliminación y asignación a usuarios.
- **Seguimiento:** estados Pendiente, En curso y Completada; prioridades Baja, Media y Alta; fecha de vencimiento opcional.
- **Búsqueda de trabajo:** filtros por proyecto, estado y prioridad, y ordenación por fecha de vencimiento o creación.
- **Mis tareas:** vista de las tareas asignadas al usuario actual.
- **Panel de indicadores:** proyectos activos y tareas abiertas, completadas y vencidas.
- **Acceso por roles:** inicio y cierre de sesión con Keycloak y permisos aplicados en la API.

| Rol | Permisos principales |
|---|---|
| `Admin` | Gestionar proyectos, tareas y asignaciones; cambiar el estado de cualquier tarea de un proyecto activo |
| `Member` | Consultar proyectos activos y sus tareas; cambiar únicamente el estado de las tareas que tenga asignadas |

Ambos roles podrán consultar el panel y su vista de tareas propias. El detalle del alcance y los criterios de aceptación se recoge en los [requisitos funcionales](docs/02-requirements/functional-requirements.md).

## Tecnologías previstas

| Área | Tecnologías |
|---|---|
| Backend | C#, ASP.NET Core y Entity Framework Core |
| Frontend | React, TypeScript y Vite |
| Navegación y consultas | TanStack Router y TanStack Query |
| Interfaz visual | Tailwind CSS y shadcn/ui |
| Persistencia | PostgreSQL |
| Identidad | Keycloak, OpenID Connect y tokens de acceso JWT |
| Entorno local | Docker y Docker Compose |
| Pruebas | xUnit, pruebas de integración de ASP.NET Core, Vitest y React Testing Library |
| Automatización y despliegue | GitHub Actions y AWS en un entorno aislado de producción |

La arquitectura propuesta separa la interfaz, la API, los datos de negocio y el proveedor de identidad. Las versiones y decisiones de implementación se concretarán durante el diseño. Consulta la [visión general del sistema](docs/03-architecture/system-overview.md) para conocer los componentes, sus responsabilidades y las decisiones pendientes.

## Plan de desarrollo

El programa se organiza en seis fases, con una duración orientativa de seis semanas:

1. Análisis de requisitos y propuesta de arquitectura.
2. Bases técnicas y preparación del entorno local.
3. Implementación de proyectos y tareas.
4. Integración de identidad, permisos y panel de indicadores.
5. Pruebas, integración continua y preparación de la entrega.
6. Despliegue en AWS, validación y entrega final.

Cada fase contará con entregables, evidencias y revisión del responsable técnico. Los objetivos y criterios de aprobación están definidos en el [plan de trabajo](docs/01-onboarding/roadmap.md).

## Documentación

| Documento | Contenido |
|---|---|
| [Descripción del programa](docs/01-onboarding/overview.md) | Contexto, alcance, equipo y dinámica de colaboración |
| [Objetivos de aprendizaje](docs/01-onboarding/objectives.md) | Competencias y criterios de finalización |
| [Plan de trabajo](docs/01-onboarding/roadmap.md) | Fases, entregables, dependencias y puntos de aprobación |
| [Seguimiento y evolución](docs/01-onboarding/evolution.md) | Evidencias de progreso y plantillas de revisión |
| [Requisitos funcionales](docs/02-requirements/functional-requirements.md) | Funcionalidades, reglas de negocio y criterios de aceptación |
| [Visión general del sistema](docs/03-architecture/system-overview.md) | Arquitectura propuesta, componentes, datos, seguridad y despliegue |

## Estado actual

El proyecto se encuentra en fase de análisis y documentación. La implementación de la aplicación, la infraestructura ejecutable y las pruebas están pendientes. Las instrucciones de instalación, ejecución y despliegue se incorporarán a medida que se completen y verifiquen los componentes.
