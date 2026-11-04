import type { Task } from "#lib/api/types";
import { Badge, Button } from "@/components/index.comp";
import { keycloak, logout } from "@/lib/auth/keycloak";

export function MyProfileFeat() {
  // USUAIRO LOGEADO
  const usuario = keycloak.tokenParsed;
  return (
    <div className="grid grid-cols-3 gap-3 p-5">
      <div className="flex flex-col gap-3 border p-5 rounded-xl bg-background justify-center">
        <div className="flex flex-col flex-1 gap-4 items-center">
          <div className="border w-35 h-35 rounded-full bg-primary flex justify-center items-center text-6xl text-white">
            {usuario?.name[0]}
          </div>
          <h1>{usuario?.preferred_username}</h1>
          <Badge>{usuario?.realm_access?.roles}</Badge>
          <h2>{usuario?.email}</h2>
        </div>
        <Button variant="outline">Editar perfil</Button>
        <Button variant="outline" className="border-red-300 text-red-800 hover:brightness-98 hover:border-red-300 hover:text-red-800" onClick={logout}>
          Cerar sesion
        </Button>
      </div>
      <div className="flex flex-col gap-4">
        <div
          id="detalles"
          className="bg-background p-5 flex flex-col gap-3 border rounded-xl"
        >
          <div className="flex gap-2">
            <h1 className="flex flex-1">Detalles</h1>
          </div>
          <div className="flex gap-2">
            <h2 className="flex flex-1">Nombre Completo</h2>
            <span>{usuario?.name}</span>
          </div>
          <div className="flex gap-2">
            <h2 className="flex flex-1">Correo electronico</h2>
            <span>{usuario?.email}</span>
          </div>
          <div className="flex gap-2">
            <h2 className="flex flex-1">Role</h2>
            <span>{usuario?.realm_access?.roles}</span>
          </div>
          <div className="flex gap-2">
            <h2 className="flex flex-1">telefolo</h2>
            <span>null</span>
          </div>
        </div>
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
      </div>
      <div
        id="LasChanges"
        className="flex flex-col gap-3 border rounded-xl overflow-hidden p-4 bg-background"
      >
        <h1>Ultimas tareas</h1>
        <div className="flex flex-col flex-1">
          <RowLastTareas />
          <RowLastTareas />
          <RowLastTareas />
          <RowLastTareas />
          <RowLastTareas />
          <RowLastTareas />
          <RowLastTareas />
          <RowLastTareas />
        </div>
      </div>
    </div>
  );
}

// LASTA CHANGE TASK row
interface parameters {
  tarea?: Task;
}
function RowLastTareas({ tarea }: parameters) {
  return (
    <div className="p-5 border-y">
      <h1>nombre de la tarea</h1>
    </div>
  );
}
