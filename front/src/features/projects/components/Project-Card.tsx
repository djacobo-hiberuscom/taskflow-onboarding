import { useState } from "react";
import { Archive, Heart, Pen, Trash, Users } from "lucide-react";
import {
  Badge,
  ContextMenu,
  ContextMenuContent,
  ContextMenuGroup,
  ContextMenuItem,
  ContextMenuTrigger,
  Separator,
} from "@/components/index.comp";
import { useProject } from "@/hooks/use-project";
import { useTaskCount } from "@/hooks/use-task";
import type { Project } from "@/lib/api/types";

interface ProjectCardProps {
  proyecto: Project;
  isAdmin: boolean;
  onOpen: (p: Project) => void;
  onEdit: (p: Project) => void;
  onArchive: (p: Project) => void;
  onDelete: (p: Project) => void;
}

// Tarjeta de proyecto (clic = abrir, clic derecho = acciones de admin)
export function ProjectCard({
  proyecto,
  isAdmin,
  onOpen,
  onEdit,
  onArchive,
  onDelete,
}: ProjectCardProps) {
  const [isFavourite, setIsFavourite] = useState(false);

  // El listado no trae miembros -> se piden al detalle (queda cacheado para la vista de detalle)
  const { data: detail } = useProject(proyecto.id);
  const { data: nTareas = 0 } = useTaskCount({ projectId: proyecto.id });
  const miembros = detail?.members.length ?? 0;

  return (
    <ContextMenu>
      <ContextMenuTrigger
        className="flex flex-col gap-4 p-4 border rounded-xl bg-background max-w-xs hover:brightness-97 cursor-pointer"
        onClick={() => onOpen(proyecto)}
      >
        <div className="flex justify-between">
          <h1 className="font-bold text-xl truncate">{proyecto.name}</h1>
          <button
            className="self-start shrink-0 bg-background rounded-full p-1 hover:brightness-95 cursor-pointer"
            onClick={(e) => {
              e.stopPropagation();
              setIsFavourite(!isFavourite);
            }}
          >
            <Heart className={isFavourite ? "fill-red-400 text-red-400" : "text-foreground"} />
          </button>
        </div>

        <p className="flex-1 line-clamp-3">{proyecto.description}</p>
        <Separator />
        <div className="flex gap-3 justify-between items-center">
          <div className="flex gap-2 justify-center items-center">
            <Users />
            <Badge className="text-[#1E40AF] py-2 bg-[#DBEAFE]">
              {miembros} miembro{miembros === 1 ? "" : "s"}
            </Badge>
          </div>
          <Badge className="bg-[#DCFCE7] text-[#166534]">{nTareas} tareas</Badge>
        </div>
      </ContextMenuTrigger>

      <ContextMenuContent>
        <ContextMenuGroup className="font-bold">
          <ContextMenuItem disabled={!isAdmin} onClick={() => onEdit(proyecto)}>
            <Pen />
            Editar
          </ContextMenuItem>
          <ContextMenuItem disabled={!isAdmin} onClick={() => onArchive(proyecto)}>
            <Archive />
            Archivar
          </ContextMenuItem>
          <ContextMenuItem
            disabled={!isAdmin}
            className="text-red-600"
            onClick={() => onDelete(proyecto)}
          >
            <Trash />
            Eliminar
          </ContextMenuItem>
        </ContextMenuGroup>
      </ContextMenuContent>
    </ContextMenu>
  );
}
