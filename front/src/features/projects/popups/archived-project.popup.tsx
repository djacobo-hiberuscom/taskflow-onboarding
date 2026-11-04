// COMPONENTES
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Button,
  Badge,
} from "@/components/index.comp";
// ICONOS
import { Archive, Trash, RefreshCw, ArrowRight } from "lucide-react";

// PROYECTOS
import type { Project } from "#lib/api/types";
import PROYECTOS from "@/assets/tempdata/projects.json";

// TAREAS
import TAREAS from "@/assets/tempdata/tasks.json"

import {getUsernameByProjectId } from "@/tools/getUsername";
import { useState } from "react";


const proyectoArchived = PROYECTOS.filter((p) => p.archived_at !== null);

interface parameters {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
}

export function ArchivedPopUp({ isOpen, onOpenChange }: parameters) {
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 5;
  const visibles = proyectoArchived.slice(
    (page - 1) * PAGE_SIZE,
    page * PAGE_SIZE,
  );
  const totalPage = Math.ceil(proyectoArchived.length / PAGE_SIZE);

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-6xl max-h-[90vh] flex flex-col p-4 gap-3">
        <DialogHeader>
          <DialogTitle className="text-2xl"> Archivados</DialogTitle>
          <DialogDescription>Proyectos archivados</DialogDescription>
        </DialogHeader>
        <div className="flex-1 overflow-y-auto">
          {visibles.map((pa) => {
            return <ArchivedRow proyecto={pa} />;
          })}
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => {
              if (page - 1 > 0) setPage(page - 1);
            }}
          >
            Anterior
          </Button>
          <h1 className="text-2xl">
            {page}/{totalPage}
          </h1>
          <Button
            variant="outline"
            onClick={() => {
              if (page + 1 <= totalPage) setPage(page + 1);
            }}
          >
            Siguiente
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// COMPONENTES DEL POPUP
interface recyclerParam {
  proyecto: Project;
}
function ArchivedRow({ proyecto }: recyclerParam) {
   const nTareas = TAREAS.filter((t) => t.project_id === proyecto.id).length;
  return (
    <div className="border-t border-b flex justify-between items-center gap-4 p-6">
      <div className="flex-1">
        <h1 className="text-xl">{proyecto.name}</h1>
        <div className="flex gap-4">
          <span>{getUsernameByProjectId(proyecto.id).length} miembros</span>
          <span>{nTareas} tareas</span>
        </div>
      </div>

      <Badge className="p-4 text-2xs bg-[#DCFCE7] text-[#166534]">
        <Archive /> Archivado {proyecto.archived_at}
      </Badge>

      <div className="flex gap-1">
        <Button variant="outline"><ArrowRight/> Abrir</Button>
        <Button variant="outline"><RefreshCw /> Restaurar</Button>
        <Button variant="outline" className="text-red-500"><Trash/> Eliminar</Button>
      </div>
    </div>
  );
}
