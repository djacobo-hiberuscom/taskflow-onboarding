# TaskFlow — Objetivos de incorporación

> **Responsable del documento:** responsable técnico
>
> **Frecuencia de revisión:** semanal
>
> **Evaluación:** individual, basada en evidencias

## 1. Objetivo general

Al finalizar el programa, cada desarrollador deberá poder realizar cambios en una aplicación profesional completa con una autonomía adecuada a su nivel: comprender requisitos, proponer un diseño mantenible, coordinar contratos, implementar, probar, documentar, revisar y explicar un despliegue en la nube.

## 2. Resultados de aprendizaje compartidos

| ID | Objetivo | Evidencia observable |
|---|---|---|
| OBJ-01 | Interpretar requisitos de negocio y desglosar el trabajo | Incidencias acotadas, criterios de aceptación y mapa de dependencias |
| OBJ-02 | Proponer y justificar una arquitectura | Diagramas, análisis de alternativas y ADR aprobados |
| OBJ-03 | Utilizar Git de forma colaborativa | Ramas por funcionalidad, confirmaciones claras, PR revisadas e incidencias vinculadas |
| OBJ-04 | Acordar y evolucionar contratos | Contrato OpenAPI versionado, ejemplos de datos e integración coordinada |
| OBJ-05 | Reproducir entornos | Otro desarrollador puede seguir el README y ejecutar el sistema |
| OBJ-06 | Aplicar autenticación y autorización correctamente | Integración con Keycloak y pruebas de acceso permitido y denegado en la API |
| OBJ-07 | Entregar software mantenible y probado | CI correcta, pruebas relevantes y cambios documentados |
| OBJ-08 | Comprender la entrega en la nube | Entorno AWS funcional; explicación del despliegue, registros, reversión, costes y retirada |
| OBJ-09 | Comunicarse profesionalmente | PR claras, bloqueos comunicados con contexto y demostraciones funcionales |

## 3. Objetivos de backend

- Explicar las responsabilidades de la API, los casos de uso, el dominio y la infraestructura de persistencia.
- Modelar proyectos, tareas y referencias a usuarios con reglas y relaciones explícitas.
- Utilizar migraciones, objetos de transferencia de datos (DTO), validación, códigos HTTP adecuados y errores coherentes.
- Validar los tokens de acceso JWT de Keycloak y autorizar operaciones por rol y recurso en la API.
- Escribir pruebas unitarias de reglas de negocio y pruebas de integración de endpoints y base de datos.
- Diagnosticar errores de configuración, registros y base de datos.
- Empaquetar y desplegar la API en un contenedor y explicar su configuración de ejecución.

**Evidencias:** documentación de arquitectura, contrato de API, endpoints probados, migraciones, resultados de CI, revisiones de PR y comprobación de salud de la API desplegada.

## 4. Objetivos de frontend

- Explicar la organización por funcionalidades, la navegación, el estado de consultas y caché, y el estado de interfaz.
- Construir componentes tipados, reutilizables y con accesibilidad básica.
- Implementar formularios con validación y estados de carga, vacío y error.
- Integrar el contrato de API acordado sin suponer formatos de respuesta.
- Integrar el flujo OIDC Authorization Code + PKCE de Keycloak para una aplicación de página única (SPA) en el navegador.
- Adaptar rutas y controles al rol, entendiendo que la API es la autoridad que decide los permisos.
- Probar las interacciones esenciales y los fallos de las solicitudes.
- Compilar y publicar los archivos estáticos; diagnosticar problemas de configuración y red en el entorno desplegado.

**Evidencias:** pantallas implementadas, documentación del frontend, pruebas, demostración integrada, revisiones de PR e interfaz desplegada.

## 5. Objetivos de calidad y colaboración

El éxito se evaluará por la calidad del resultado. Cada desarrollador deberá:

- Completar las incidencias aceptadas y demostrarlas con datos reales en el MVP final.
- Dar y aplicar comentarios de revisión concretos.
- Identificar al menos una decisión arquitectónica relevante y sus ventajas e inconvenientes.
- Resolver un defecto desde su reproducción hasta la corrección y la comprobación de regresión.
- Explicar el recorrido completo desde el navegador hasta la API y la base de datos, y la respuesta al usuario.
- Identificar al menos un riesgo operativo: secretos, CORS, manejo de tokens, copias de seguridad, registros, HTTPS o costes de AWS.

## 6. Criterios de finalización de cada incidencia

- Criterios de aceptación satisfechos, incluidos errores y casos límite cuando proceda.
- Cambios vinculados a la incidencia y presentados mediante una PR.
- Pruebas relevantes añadidas o justificación documentada cuando no correspondan.
- Comprobaciones de CI correctas y verificación local realizada.
- Contrato de API y documentación actualizados si cambian las interfaces o el comportamiento.
- Aprobación de al menos un revisor y ausencia de comentarios bloqueantes sin resolver.
- Cambios integrados, incidencia marcada como finalizada y demostración o evidencia adjunta cuando resulte útil.

## 7. Criterios de finalización del programa

El responsable técnico aprobará la finalización cuando se cumplan estos puntos:

1. Propuesta de arquitectura y decisiones principales revisadas y trazables.
2. Ambos desarrolladores pueden preparar el entorno local de forma independiente.
3. El MVP supera las pruebas de aceptación, incluidos los accesos que deben denegarse.
4. CI correcta en `main` e instrucciones para reproducir las pruebas.
5. Demostración en AWS accesible mediante URL HTTPS aprobadas, sin secretos expuestos.
6. El equipo puede explicar los registros y la supervisión, el despliegue, la reversión y la retirada de recursos.
7. Cada desarrollador explica técnicamente el componente del otro.

Finalizar el programa no implica autonomía de nivel sénior. Los objetivos de aprendizaje pendientes se recogerán en el [seguimiento de evolución](evolution.md).
