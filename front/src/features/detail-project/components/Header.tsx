import { Badge } from "@/components/index.comp";
import { useTaskCount } from "@/hooks/use-task";
import type { ProjectDetail } from "@/lib/api/types";
import { FunButtons } from "./FunButtons";
import { ProjectOwner } from "./ProjectOwner";
import type { DetailProjectPopup } from "./types";

interface HeaderProps {
  proyecto: ProjectDetail;
  open: (popup: DetailProjectPopup) => void;
}

// Cabecera: nombre, descripción, nº de tareas, owner y acciones
export function Header({ proyecto, open }: HeaderProps) {
  const { data: nTareas = 0 } = useTaskCount({ projectId: proyecto.id });

  return (
    <div className="p-5 w-full flex justify-between">
      <div>
        <h1 className="text-3xl font-bold">{proyecto.name}</h1>
        <p>{proyecto.description}</p>
        <Badge className="mt-9 text-2xs font-bold text-[#166534] py-2 bg-[#DCFCE7]">
          {nTareas} Tareas
        </Badge>
      </div>
      <div className="flex flex-col justify-end gap-3">
        <ProjectOwner name={proyecto.createdBy?.displayName} />
        <FunButtons open={open} />
      </div>
    </div>
  );
}
