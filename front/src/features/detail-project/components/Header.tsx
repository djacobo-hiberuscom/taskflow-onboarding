// TAREAS DEL PROYECTO
import TAREAS from "@/assets/tempdata/tasks.json";



// TYPES
import type { Project } from "#lib/api/types";

// Components
import { Badge, Button,  } from "@/components/index.comp";
import { FunButtons, ProjectOwner } from "./index";
// ICONS
import {  Plus,  } from "lucide-react";

// Parametros
interface params {
  proyecto: Project | undefined;
  editClic: () => void;
  archiveClic: () => void;
  deleteClic: () => void;
}

// Componente
export function Header({
  proyecto,
  editClic,
  archiveClic,
  deleteClic,
}: params) {
  const projectTask = TAREAS.filter((t) => t.project_id === proyecto?.id);

  return (
    <div className="p-5 w-full flex justify-between">
      <div id="texto">
        <h1 className="text-3xl font-bold">{proyecto?.name}</h1>
        <p> {proyecto?.description}</p>

        <Badge className="mt-9 text-2xs font-bold text-[#166534] py-2 bg-[#DCFCE7]">
          {projectTask.length} Tareas
        </Badge>
      </div>
      <div id="botonera" className="flex flex-col justify-end items gap-3">
        <ProjectOwner name={proyecto?.createdBy?.displayName}/>
        <FunButtons
          editClic={editClic}
          archiveClic={archiveClic}
          deleteClic={deleteClic}
        />
        <Button>
          <Plus />
          Crear tarea
        </Button>
      </div>
    </div>
  );
}
