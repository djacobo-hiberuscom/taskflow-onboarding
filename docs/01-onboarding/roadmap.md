# TaskFlow — Plan de trabajo de incorporación

> **Duración:** 6 semanas orientativas, ajustables según la experiencia
>
> **Seguimiento:** iteraciones de GitHub Projects y PR vinculadas
>
> **Destino de la entrega:** cuenta AWS de pruebas o desarrollo, aislada de producción

## Flujo de entrega y puntos de aprobación

Cada fase tendrá un resultado, evidencias y un punto de aprobación del responsable técnico. La implementación comenzará tras aceptar la propuesta de arquitectura. Si una fase no supera la revisión, se corregirá y se revisarán las prioridades; el calendario no se ampliará automáticamente.

| Fase | Plazo orientativo | Criterio de aprobación |
|---|---|---|
| 0. Análisis y documentación | Semana 1 | Propuesta de arquitectura aceptada |
| 1. Bases técnicas y entorno local | Semana 2 | Sistema inicial reproducible |
| 2. MVP de proyectos y tareas | Semana 3 | Primer recorrido de negocio completo aceptado |
| 3. Identidad, permisos y panel | Semana 4 | MVP integrado y protegido aceptado |
| 4. Calidad, CI y preparación de entrega | Semana 5 | Versión candidata aprobada |
| 5. Despliegue AWS y entrega | Semana 6 | Demostración en la nube, guía operativa y plan de retirada aprobados |

## Fase 0 — Análisis, requisitos y propuesta de arquitectura (semana 1)

**Objetivo:** comprender el problema de negocio y diseñar el sistema antes de implementar funcionalidades.

**Ambos desarrolladores**

- Leer la [descripción](overview.md), los [objetivos](objectives.md) y los [requisitos funcionales](../02-requirements/functional-requirements.md); redactar las normas de contribución pendientes.
- Enumerar recorridos de usuario y supuestos; registrar preguntas abiertas como incidencias.
- Definir el alcance del MVP, las historias de usuario y los criterios de aceptación.
- Preparar una propuesta de arquitectura que incluya:
  - Contexto y componentes: navegador con SPA React, API ASP.NET Core, Keycloak y PostgreSQL.
  - Responsabilidades de cada componente y comunicaciones entre ellos.
  - Organización del repositorio y límites de dependencia.
  - Autenticación OIDC Authorization Code + PKCE, validación de JWT y permisos por rol y recurso.
  - Modelo entidad-relación, claves, relaciones y estrategia de migraciones.
  - Contrato inicial de API: endpoints, DTO, códigos HTTP y errores.
  - Entorno local con Docker Compose, configuración y puertos.
  - Estrategia de pruebas y comprobaciones de CI.
  - Propuesta AWS con servicios, estimación de costes, seguridad, complejidad operativa y retirada de recursos.
  - Comparación de al menos dos opciones de despliegue y justificación de la propuesta.
- Registrar las decisiones relevantes en ADR breves y señalar las cuestiones sin resolver.

**Responsable de backend:** capas de la API, entidades, persistencia, límite de seguridad y contrato de API.

**Responsable de frontend:** estructura de SPA, navegación, estado de consultas, flujo OIDC y recorridos de interfaz.

**Responsable técnico:** revisar los supuestos y aceptar o rechazar la propuesta.

**Documentación requerida para cerrar la fase:** completar la [visión general del sistema](../03-architecture/system-overview.md) y crear `docs/03-architecture/backend.md`, `frontend.md`, `database.md`, `authentication.md` y `deployment-aws.md`; crear también `docs/05-api/api-contracts.md`, al menos un ADR en `docs/03-architecture/adr/` y `CONTRIBUTING.md`.

Estas rutas describen entregables pendientes; actualmente solo existe `system-overview.md` dentro de la documentación de arquitectura.

**Aprobación de fase 0:** arquitectura viable y comprensible; modelo de datos y límites de seguridad coherentes; contrato suficiente para trabajar en paralelo; presupuesto máximo y plan de retirada de AWS definidos; comentarios bloqueantes resueltos. Después comenzará la implementación.

## Fase 1 — Bases técnicas y entorno local (semana 2)

**Backend**

- Crear la solución ASP.NET Core con los límites de capas o módulos acordados.
- Configurar EF Core, conexión a PostgreSQL, primera migración y datos iniciales.
- Añadir comprobación de salud, gestión uniforme de errores y contrato OpenAPI.
- Preparar la configuración sin incluir secretos en Git.

**Frontend**

- Crear el proyecto Vite/React/TypeScript con la estructura aprobada.
- Configurar navegación, cliente de consultas, Tailwind y shadcn/ui.
- Construir la estructura adaptable, la navegación y las pantallas iniciales de proyectos y tareas.
- Crear un cliente de API tipado y respuestas simuladas basadas en el contrato.

**Trabajo compartido**

- Preparar Docker Compose con PostgreSQL y Keycloak, con bases de datos lógicas y credenciales separadas para TaskFlow y Keycloak.
- Publicar `.env.example` con valores de ejemplo, e instrucciones de arranque y reinicio.
- Preparar la importación del dominio de identidad de Keycloak (realm), los usuarios de demostración, roles y URL de retorno.
- Crear las primeras PR y revisarlas; documentar los pasos en el futuro `docs/04-development/environment-setup.md`.

**Aprobación de fase 1:** cualquiera de los desarrolladores puede clonar, configurar e iniciar las dependencias; la API responde a la comprobación de salud; la interfaz se muestra; las migraciones funcionan sobre una base vacía; las instrucciones están revisadas.

## Fase 2 — MVP de proyectos y tareas (semana 3)

**Backend**

- Implementar creación, consulta, edición y archivado de proyectos, sin introducir borrado de proyectos fuera del alcance.
- Implementar creación, consulta, edición, borrado y asignación de tareas, con proyecto, estado y prioridad.
- Añadir filtros y ordenación exigidos; acordar la paginación si es necesaria, errores coherentes y pruebas relevantes.

**Frontend**

- Implementar listas y formularios de proyectos y tareas, archivado de proyectos, borrado de tareas y asignación.
- Añadir validación, confirmaciones, filtros y estados de carga, vacío y error.
- Sustituir las respuestas simuladas por llamadas reales y coordinar los cambios de contrato.

**Trabajo compartido**

- Demostrar: crear proyecto → añadir tarea → cambiar estado → recargar → comprobar persistencia.
- Añadir pruebas de integración de API e interacciones básicas de interfaz.
- Mientras la autorización no esté terminada, ejecutar esta fase únicamente en un entorno local aislado. No publicar operaciones de negocio desprotegidas en AWS.

**Aprobación de fase 2:** recorrido completo con PostgreSQL, reproducible desde un entorno nuevo y trazable mediante incidencias y PR. Esta demostración todavía no acredita el MVP protegido de la fase 3.

## Fase 3 — Keycloak, autorización y panel (semana 4)

**Backend**

- Validar firma, emisor, audiencia y vigencia de los tokens de acceso de Keycloak; configurar la audiencia de la API en Keycloak.
- Aplicar los permisos de `Admin` y `Member`, incluidas las comprobaciones de asignación para los cambios de estado de `Member`.
- Denegar operaciones a identidades sin un rol admitido.
- Probar solicitudes sin autenticar, con tokens caducados o inválidos y sin permisos suficientes.

**Frontend**

- Integrar OIDC Authorization Code + PKCE con un cliente público de SPA; inicio y cierre de sesión y retorno a la aplicación.
- Gestionar la caducidad de sesión, las rutas protegidas y los controles según el rol.
- Implementar «Mis tareas» y el panel de proyectos activos y tareas abiertas, completadas y vencidas con datos reales.

**Trabajo compartido**

- Acordar orígenes CORS, URL, manejo de tokens y zona horaria para los vencimientos.
- Mantener los secretos de clientes confidenciales fuera de los archivos de la SPA.
- Probar los recorridos de `Admin` y `Member`, incluidas las llamadas directas a la API que deben denegarse.

**Aprobación de fase 3:** accesos legítimos operativos, respuestas 401/403 coherentes, permisos aplicados en la API y panel integrado. Los proyectos archivados y sus tareas quedan excluidos de las vistas activas y los recuentos.

## Fase 4 — Calidad, CI y versión candidata (semana 5)

**Backend**

- Completar las pruebas unitarias y de integración, con PostgreSQL cuando se verifique persistencia o consultas específicas.
- Revisar eficiencia de consultas EF Core, validación, registros, cancelación y errores.
- Construir una imagen de ejecución mínima con usuario sin privilegios cuando sea viable.

**Frontend**

- Probar pantallas principales y errores; revisar navegación por teclado y accesibilidad básica.
- Retirar código sin uso y simulaciones de los recorridos entregados; comprobar la compilación de entrega.
- Empaquetar los archivos estáticos y documentar cómo se configuran los endpoints por entorno.

**Trabajo compartido**

- Configurar GitHub Actions para compilar, analizar o comprobar formato cuando proceda, probar ambos componentes y generar los artefactos.
- Exigir comprobaciones en las PR; preparar verificaciones básicas de entrega, versión o etiqueta y borrador de guía de despliegue.
- Definir reversión del despliegue y ejecución de migraciones, con pasos reproducibles y versionados.

**Aprobación de fase 4:** CI correcta en `main`, compilación reproducible, cobertura de regresiones de integración y permisos, y despliegue y reversión documentados.

## Fase 5 — Despliegue AWS, validación y entrega (semana 6)

**Objetivo:** demostrar un despliegue real fuera de producción con un coste y una complejidad adecuados al ejercicio.

### Paso A — Confirmación de arquitectura y presupuesto

- Confirmar con el responsable técnico la arquitectura elegida en fase 0, región, cuenta y límite mensual de gasto antes de crear recursos.
- Comparar una opción sencilla con EC2, contenedores de API y Keycloak, e interfaz mediante un servidor HTTPS; PostgreSQL podrá residir en el mismo host solo para formación, o en una instancia RDS privada si se aprueba el presupuesto.
- Evaluar una alternativa con ECS Fargate, ECR, RDS y S3/CloudFront si el tiempo y el presupuesto permiten su complejidad. Contabilizar balanceador, NAT, registros y demás recursos usados.
- Si se elige S3 para la interfaz, servir un origen S3 privado mediante CloudFront y control de acceso al origen (OAC). El endpoint de sitio web de S3 por sí solo no ofrece HTTPS.
- Definir red, grupos de seguridad, permisos mínimos y nombres HTTPS aprobados. No publicar PostgreSQL, interfaces administrativas de Keycloak ni puertos de gestión de contenedores.

### Paso B — Aprovisionamiento y despliegue

- Crear un entorno aislado con recursos identificados y etiquetas, por ejemplo `Project=TaskFlow`, `Environment=Onboarding` y `Owner=...`.
- Configurar roles IAM de instancia o tarea y almacenar secretos fuera de Git, por ejemplo en SSM Parameter Store o Secrets Manager según lo aprobado.
- Configurar HTTPS, nombre público de Keycloak y URL de retorno del frontend.
- Aplicar migraciones revisadas, iniciar Keycloak y publicar API e interfaz.
- Verificar CORS, URL de retorno OIDC, URL de API, emisor y audiencia de los tokens.
- Registrar comandos y parámetros en el futuro `docs/04-development/aws-deployment.md`.

### Paso C — Verificaciones funcionales, operativas y de coste

- Probar inicio y cierre de sesión, recorridos de ambos roles y acciones denegadas desde un navegador externo.
- Revisar salud de API, registros de contenedores y Keycloak, y conexión a base de datos.
- Demostrar reinicio, nuevo despliegue y reversión. Revisar la compatibilidad con el esquema y evitar revertir migraciones a ciegas.
- Configurar alertas de presupuesto, retirar reglas temporales de entrada y definir las expectativas de copia y recuperación. Las alertas no constituyen un límite automático de gasto y pueden llegar después de incurrir en nuevos cargos, como explica la [documentación de AWS Budgets](https://docs.aws.amazon.com/cost-management/latest/userguide/budgets-managing-costs.html).
- Documentar inventario, pasos de retirada y responsable de apagar los servicios con coste.

### Paso D — Entrega final

- Demostrar la autenticación del navegador con Keycloak y las solicitudes de la SPA a la API, que accede a PostgreSQL.
- Mostrar CI, trazabilidad entre incidencias y PR, registros en la nube y diagnóstico de fallos.
- Completar README, diagramas, contrato de API, guía operativa, riesgos y seguimiento.
- Obtener la aceptación del responsable técnico y acordar quién conserva el entorno y asume su coste, o ejecutar la retirada aprobada.

**Aprobación de fase 5:** aplicación accesible por HTTPS, recorridos probados en AWS, ausencia de credenciales en Git, exposición de red controlada, registros y nuevo despliegue demostrados, y costes y retirada documentados.

## Organización del trabajo y dependencias

Épicas propuestas para GitHub:

- **EPIC-01:** análisis, requisitos y arquitectura — fase 0.
- **EPIC-02:** entorno local y bases técnicas — fase 1.
- **EPIC-03:** MVP de proyectos y tareas — fase 2.
- **EPIC-04:** seguridad y panel — fase 3.
- **EPIC-05:** pruebas, CI y preparación de entrega — fase 4.
- **EPIC-06:** despliegue AWS y entrega — fase 5.

Usar etiquetas como `area:backend`, `area:frontend`, `area:infrastructure`, `type:documentation`, `type:feature` y `type:bug`, y registrar dependencias. El frontend podrá utilizar simulaciones tipadas del contrato aprobado mientras se completa el backend.

## Riesgos y control del alcance

| Riesgo | Respuesta |
|---|---|
| La arquitectura consume demasiado tiempo | Limitar la exploración y revisar el borrador a mitad de semana |
| El frontend queda bloqueado por el backend | Acordar pronto el contrato y usar simulaciones tipadas |
| Keycloak retrasa el MVP | Configurar el realm en fase 1 y completar la autorización en fase 3 |
| El coste AWS supera el valor formativo | Aprobar presupuesto antes de crear recursos y reducir la complejidad |
| HTTPS o permisos bloquean la entrega | Tratarlos como requisitos de aprobación del despliegue |
| Seis semanas resultan insuficientes | Replanificar alcance y acabados visuales preservando pruebas, seguridad y entrega en la nube |

## Informe semanal

Cada viernes se enlazarán incidencias terminadas, PR integradas, demostraciones, pruebas, bloqueos, decisiones y prioridades siguientes. Registrar la evolución conforme al [documento de seguimiento](evolution.md), manteniendo las valoraciones personales en el espacio restringido.
