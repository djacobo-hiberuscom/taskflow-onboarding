// Keycloak
import type { KeycloakTokenParsed } from "keycloak-js";

// Param
interface param{
    usuario?: KeycloakTokenParsed | undefined
}

// Componente
export function StatsMe({usuario}:param) {
  return (
    <div
      id="stats"
      className="bg-background p-5 flex flex-col gap-3 border rounded-xl"
    >
      <h1>Mis estadisticas / Actividad</h1>
      <div>
        <div className="flex gap-2">
          <h2 className="flex flex-1">Tareas completadas</h2>
          <span>N</span>
        </div>
        <div className="flex gap-2">
          <h2 className="flex flex-1">Tareas pendientes</h2>
          <span>N</span>
        </div>
        <div className="flex gap-2">
          <h2 className="flex flex-1">Proyectos</h2>
          <span>N</span>
        </div>
      </div>
    </div>
  );
}
