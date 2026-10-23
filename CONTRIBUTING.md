# Contribuir a TaskFlow

Este documento establece el flujo de trabajo del equipo: una rama por cambio, una pull request para revisarlo e integración en `main` después de la revisión.

## 1. Ramas y alcance

- `main` es la rama de integración y debe mantenerse utilizable según la fase del proyecto.
- Cada funcionalidad, corrección o cambio de documentación se desarrolla en una rama creada desde `main` actualizada.
- Cada rama y PR tiene un objetivo concreto. Una funcionalidad grande se divide en entregas pequeñas que puedan revisarse por separado.
- Antes de empezar, registrar o elegir una incidencia con su objetivo, criterios de aceptación y requisitos `FR-###` relacionados cuando corresponda.

Convención de nombres, con ejemplos ilustrativos:

| Tipo de cambio | Rama de ejemplo |
|---|---|
| Funcionalidad | `feature/123-crear-proyecto` |
| Corrección | `fix/124-validar-vencimiento` |
| Documentación | `docs/125-arquitectura-backend` |
| Configuración o mantenimiento | `chore/126-entorno-local` |

El número hace referencia a la incidencia cuando exista. Los nombres deben ser cortos y describir el trabajo.

## 2. Flujo de una contribución

1. Acordar el alcance y los criterios de aceptación de la incidencia.
2. Actualizar la referencia de `main` y crear la rama del cambio.
3. Realizar cambios centrados en ese objetivo, con commits comprensibles.
4. Ejecutar las comprobaciones aplicables y actualizar la documentación afectada.
5. Publicar la rama y abrir una PR hacia `main`. Puede abrirse como borrador para recibir comentarios antes de terminar.
6. Solicitar revisión al compañero y responder a los comentarios.
7. Resolver los comentarios bloqueantes y obtener al menos una aprobación de otra persona. Para decisiones de arquitectura, requisitos o alcance, solicitar también la revisión del responsable técnico prevista en el programa.
8. Integrar mediante **Squash and merge**, con un título que describa el cambio final. Eliminar la rama de trabajo después de la integración.

**Squash** agrupa todos los commits de la PR en un único commit nuevo en `main`, que contiene el resultado completo del cambio. Por ejemplo, los commits «crear endpoint», «añadir prueba» y «corregir revisión» se integran como un solo commit «Crear proyecto y pertenencia inicial». No elimina las funcionalidades de esos commits; resume su historial de integración.

No se integra un cambio solo porque el autor haya terminado de escribirlo: debe cumplir los criterios de aceptación y tener la revisión correspondiente. Las incidencias completadas quedan vinculadas a su PR.

## 3. Contenido de las PR

La descripción debe permitir revisar el cambio sin reconstruir conversaciones anteriores. Incluir:

- Problema que se resuelve y comportamiento resultante.
- Incidencia y requisitos relacionados.
- Comprobaciones realizadas y resultado. Indicar con claridad lo que todavía no se ha podido verificar.
- Cambios en contrato, modelo de datos, configuración o documentación cuando existan.
- Capturas para cambios visuales cuando ayuden a la revisión.

Ejemplo de estructura:

```markdown
## Cambio

Qué problema resuelve y cuál es el comportamiento esperado.

## Relación

Closes #123. Requisitos: FR-006, FR-023.

## Verificación

Comprobaciones ejecutadas y resultado; limitaciones pendientes.
```

## 4. Coordinación entre backend y frontend

- Los cambios del contrato se acuerdan entre ambos y se reflejan en [api-contracts.md](docs/05-api/api-contracts.md).
- Una modificación de campos, rutas, errores o permisos debe explicar su efecto sobre el otro componente.
- Las simulaciones del frontend siguen el contrato acordado y se retiran de los recorridos de entrega al integrarlos con la API real.
- Los cambios de requisitos se proponen y se registran en [documentation-alignment.md](docs/03-architecture/documentation-alignment.md). Por indicación del usuario, los documentos originales del manager (`system-overview.md`, `functional-requirements.md` y `roadmap.md`) no se editan hasta obtener su aprobación; después se trasladan los acuerdos y se actualiza ese seguimiento.
- Los [ADR](docs/03-architecture/adr/README.md) se completarán conforme avance el trabajo, conservando su proceso de revisión y sus estados.
- El esquema físico tiene como fuente [database.md](docs/03-architecture/database.md); el diseño de las capas se documenta en [backend.md](docs/03-architecture/backend.md).
- Las ideas posteriores al MVP se reúnen en el [backlog de mejoras](docs/backlog.md), sin ampliar automáticamente el alcance actual.

## 5. Comprobaciones según la fase

### Durante la fase de documentación

- Revisar coherencia con requisitos y decisiones ya acordadas.
- Comprobar enlaces, tablas y diagramas modificados.
- Distinguir una propuesta de una decisión aceptada y de una funcionalidad implementada.
- No presentar como ejecutadas pruebas de una aplicación que todavía no existe.

### Cuando exista implementación

- Backend: restauración de paquetes, compilación y pruebas relevantes para el cambio.
- Frontend: instalación reproducible de dependencias, compilación y comprobaciones relevantes.
- Cambios de persistencia: migración correspondiente y verificación sobre PostgreSQL, incluyendo una base vacía cuando corresponda.
- Cambios de permisos: comprobaciones de operaciones permitidas y denegadas.
- Mantener correctas las comprobaciones de CI disponibles antes de integrar la PR.

Los comandos concretos se documentarán al crear y verificar cada componente. Este documento no implica que ya existan CI, pruebas automatizadas o protecciones de rama configuradas en GitHub. Se propone proteger `main` para exigir PR, revisión y las comprobaciones que se incorporen.

## 6. Configuración y datos

- No versionar tokens, contraseñas ni archivos con secretos reales. Usar archivos de ejemplo con nombres de variables y valores de demostración no sensibles.
- Aspire AppHost será el orquestador de desarrollo. Su diseño y los pasos que se verificarán en fase 1 están en [environment-setup.md](docs/04-development/environment-setup.md); las migraciones se aplican explícitamente por separado.
- Mantener separadas las bases y credenciales de TaskFlow y Keycloak.
- No modificar directamente las tablas internas de Keycloak desde el backend de negocio.
- Las operaciones que eliminan datos locales o volúmenes deben estar identificadas y explicadas en la guía de entorno.

## 7. Cierre de una contribución

Una contribución está terminada cuando cumple sus criterios de aceptación, tiene verificación adecuada a la fase, incluye la documentación necesaria y ha sido revisada e integrada en `main`.

La integración de código no autoriza por sí sola un despliegue. La preparación del despliegue y la documentación AWS se abordarán en su momento, según la planificación acordada.
