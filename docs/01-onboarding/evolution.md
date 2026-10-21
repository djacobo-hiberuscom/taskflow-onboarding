# TaskFlow — Evolución y seguimiento de los desarrolladores

> **Propósito:** acompañamiento continuo y seguimiento del progreso mediante evidencias
>
> **Responsable:** responsable técnico, con aportaciones de ambos desarrolladores
>
> **Frecuencia:** actualizaciones diarias en GitHub y reunión semanal de revisión

## 1. Uso del documento

Este archivo describe el sistema de seguimiento y proporciona plantillas reutilizables. Las valoraciones personales y las notas sensibles de acompañamiento se guardarán en una ubicación corporativa con acceso restringido. Las incidencias y PR de GitHub serán la fuente principal de evidencias técnicas.

GitHub Projects reflejará el estado del trabajo; este documento recogerá los objetivos de aprendizaje y la evolución. Se evitará duplicar el estado de las tareas en Markdown.

## 2. Flujo de trabajo en GitHub Projects

Estados propuestos: **Pendiente → Preparado → En curso → En revisión → Finalizado**, con una etiqueta adicional para los bloqueos. Al configurar el tablero se documentará la equivalencia si se utilizan los nombres `Backlog`, `Ready`, `In Progress`, `In Review` y `Done`.

Cada incidencia contendrá objetivo, alcance, responsable, fase o épica vinculada, criterios de aceptación, dependencias, validación sugerida y estimación de esfuerzo. Cada PR se vinculará a su incidencia y explicará las pruebas y los riesgos. La rama `main` deberá estar protegida.

El número de confirmaciones, las líneas de código o los puntos de historia no se utilizarán como indicadores de productividad individual.

## 3. Registro semanal de progreso

Copiar la siguiente plantilla por semana. Los comentarios sobre rendimiento individual se registrarán en el espacio restringido.

```markdown
### Semana X — AAAA-MM-DD a AAAA-MM-DD

**Fase y resultado previsto:**
**Demostración o enlace a evidencia:**

| Trabajo | Responsable | Estado | PR o evidencia | Notas técnicas |
|---|---|---|---|---|
| #... | Backend | Finalizado / Bloqueado / En revisión | #... | ... |
| #... | Frontend | Finalizado / Bloqueado / En revisión | #... | ... |

**Decisiones de arquitectura o contrato:**

- ...

**Hallazgos de integración o calidad:**

- ...

**Bloqueos y medidas:**

- Bloqueo / responsable / próxima acción / fecha objetivo

**Acciones de mejora del equipo:**

- ...

**Prioridades de la próxima semana:**

1. ...
2. ...
```

## 4. Rúbrica de competencias

Cada valoración debe apoyarse en evidencias. Una puntuación baja identifica un área de desarrollo.

| Puntuación | Comportamiento observable |
|---|---|
| 1 — Con guía | Necesita ayuda paso a paso en la mayoría de las tareas del área |
| 2 — En desarrollo | Completa tareas conocidas con comprobaciones frecuentes |
| 3 — Competente | Entrega tareas habituales con revisión periódica |
| 4 — Autónomo | Resuelve casos ambiguos, identifica riesgos y pide ayuda con preguntas concretas |
| 5 — Capaz de acompañar | Explica las alternativas y ayuda a otros en esta competencia |

| Categoría | Peso | Evidencias que revisar |
|---|---:|---|
| Entrega técnica | 30 % | Funcionalidades operativas, comportamiento correcto y comprensión de la arquitectura |
| Calidad del código y pruebas | 25 % | PR revisadas, pruebas, mantenibilidad y correcciones |
| Autonomía y resolución de problemas | 20 % | Investigaciones, propuestas y peticiones de ayuda con contexto |
| Colaboración | 15 % | Coordinación de contratos, revisión de código y comunicación |
| Planificación y documentación | 10 % | Desglose de incidencias, estado fiable y claridad de ADR y guías operativas |

La puntuación ponderada es opcional: suma de cada puntuación multiplicada por su peso. Se utilizará como apoyo al acompañamiento, con ejemplos concretos, sin convertirla en una decisión laboral automatizada.

## 5. Preguntas para la revisión semanal

1. ¿Qué incidencia aceptada demuestra mejor el progreso y por qué?
2. ¿Qué has aprendido sobre arquitectura, API o autenticación?
3. ¿Qué error o decisión de diseño fue más difícil y cómo lo investigaste?
4. ¿Qué dependencia con el otro desarrollador funcionó bien o mal?
5. ¿Qué cambiarás en tu forma de trabajar la próxima semana?
6. ¿Qué apoyo necesitas del responsable técnico?

## 6. Matriz de evolución por hitos

| Hito | Evidencia de backend | Evidencia de frontend | Evidencia del equipo | ¿Aprobado? |
|---|---|---|---|---|
| Fase 0: arquitectura | Capas, modelo entidad-relación y propuesta de API | Estructura de interfaz, navegación y autenticación | Diagramas, ADR y comparación de opciones AWS | ☐ |
| Fase 1: bases técnicas | API, migraciones y base de datos local | Estructura de SPA y navegación | Entorno local reproducible | ☐ |
| Fase 2: MVP | Endpoints de proyectos y tareas con pruebas | Pantallas de gestión con datos reales | Demostración integrada con persistencia | ☐ |
| Fase 3: seguridad | Pruebas de JWT, roles y recursos | OIDC y controles de acceso en la interfaz | Recorridos de `Admin` y `Member` | ☐ |
| Fase 4: calidad | Pruebas de integración e imagen de contenedor | Pruebas de interfaz y compilación | CI y lista de comprobaciones de entrega | ☐ |
| Fase 5: AWS | API, base de datos y registros desplegados | Interfaz desplegada y retorno de OIDC | Demostración HTTPS, reversión y retirada | ☐ |

## 7. Criterios de una revisión eficaz

- Referenciar incidencias, PR, pruebas o demostraciones concretas.
- Distinguir el comportamiento entregado del proceso utilizado para conseguirlo.
- Acordar un máximo de dos objetivos de desarrollo por persona y semana.
- Considerar dependencias imprevistas y cambios de alcance antes de valorar el progreso.
- Tratar en privado los comentarios sobre rendimiento individual.

## 8. Evaluación final y entrega

Cada desarrollador realizará una explicación de su componente de unos diez minutos y participará en una demostración conjunta. Se añadirá una petición pequeña de cambio entre componentes para comprobar la comprensión del sistema completo.

**Lista de comprobación final**

- [ ] Seis puntos de aprobación registrados con evidencias.
- [ ] Pruebas de aceptación del MVP correctas en local y en AWS.
- [ ] CI correcta en `main`; documentación que permite reproducir el entorno y el despliegue.
- [ ] Arquitectura AWS, costes, registros, reversión y retirada documentados.
- [ ] Cada desarrollador puede explicar la autenticación con el proveedor de identidad y el acceso del navegador a la API y a la base de datos.
- [ ] Riesgos pendientes y próximos objetivos de aprendizaje identificados.
- [ ] Responsable técnico conforme y titularidad del entorno AWS o retirada de recursos confirmada.

**Registro final:** decisión de finalización o ampliación con objetivos, enlaces a evidencias, fortalezas, prioridades de aprendizaje, siguiente asignación acordada y fecha de revisión. Los detalles personales se mantendrán en un sistema con acceso restringido.
