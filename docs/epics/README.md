# TaskFlow — Épicas (plan de una semana)

> **Responsables:** David (backend y base de datos) y Oliver (frontend y Keycloak).
>
> **Alcance:** implementación del MVP en **5 días** sobre la documentación ya definida en [`docs/`](../), que no se modifica. Este plan sustituye al reparto en seis fases del [roadmap](../01-onboarding/roadmap.md) para la parte de código.

## Épicas

| Épica | Calendario | Issues | David | Oliver | Conjunta |
|---|---|:---:|:---:|:---:|:---:|
| [EPIC-01 — Entorno Aspire y bases técnicas](EPIC-01-entorno-y-bases.md) | Días 1–2 | 4 | 10 h | 10 h | 0 h |
| [EPIC-02 — MVP de proyectos y tareas](EPIC-02-mvp-proyectos-y-tareas.md) | Días 2–5 | 7 | 23 h | 20 h | 0 h |
| [EPIC-03 — Seguridad, panel e integración final](EPIC-03-seguridad-panel-e-integracion.md) | Días 2–5 | 6 | 8 h | 10 h | 3 h |
| **Total** | | **17** | **41 h** | **40 h** | **3 h** |

## Orden de trabajo en paralelo

La orquestación con Aspire va primero (día 1) y de ella cuelgan los dos carriles. Después cada uno trabaja por su lado y solo se cruzan en los puntos marcados.

| Día | David (backend y BD) | Oliver (frontend y Keycloak) |
|:---:|---|---|
| 1 | ISS-01 apphost-db | ISS-03 keycloak-realm |
| 2 | ISS-02 backend-base<br>ISS-05 domain-model | ISS-04 spa-base<br>ISS-15 oidc-session |
| 3 | ISS-12 jwt-auth | ISS-09 projects-ui |
| 4 | ISS-06 projects-api | ISS-10 tasks-ui |
| 5 | ISS-07 tasks-api<br>ISS-08 task-queries<br>ISS-13 introspection<br>ISS-14 dashboard-api | ISS-11 assign-status-ui<br>ISS-16 dashboard-profile-ui · conjunta: ISS-17 |

### Cruces entre carriles

| Qué | Quién espera |
|---|---|
| ISS-01 y ISS-03 (día 1): el AppHost y el realm | Ambos necesitan arrancar el entorno antes de seguir |
| ISS-12 (día 3): API protegida | David usa identidad real en ISS-06; Oliver puede integrar contra la API |
| ISS-06 y ISS-07 (días 4–5): endpoints reales | Oliver sustituye las simulaciones por la API real |
| ISS-17 (día 5): integración | Ambos cierran juntos |

Oliver trabaja con simulaciones del contrato ([api-contracts.md](../05-api/api-contracts.md)) hasta que cada endpoint esté disponible. Cualquier cambio de contrato se refleja en ese documento en la misma PR.

## Capacidad y qué se recorta

Capacidad de referencia: 40 h por persona (5 días de 8 h). Las estimaciones suponen desarrollo asistido por IA, con pruebas incluidas en cada issue y sin tiempo de margen.

| | Total | De ellas P0 | Capacidad |
|---|:---:|:---:|:---:|
| David | 41 h | 37 h | 40 h |
| Oliver | 40 h | 36 h | 40 h |

Con la conjunta (3 h cada uno) David queda en 44 h y Oliver en 43 h: **sin holgura**. Orden de recorte si hay retraso:

1. ISS-13 (introspección, P2): FR-002 queda con validación solo local del token.
2. Panel y perfil (ISS-14 y ISS-16, P1): se entregan al final o se aplazan.
3. Pruebas de interacción más allá de las críticas de cada issue.

## Fuera de esta semana

- **FR-020, despliegue en AWS**, incluida la guía operativa y la retirada: se mantiene como está definido en el roadmap (fase 5) y requiere aprobación previa de presupuesto.
- **GitHub Actions y protección de `main`**, accesibilidad ampliada, pruebas extremo a extremo y procedimientos de mantenimiento de usuarios: no se planifican para esta semana.
- Mejoras posteriores: [backlog](../backlog.md).
