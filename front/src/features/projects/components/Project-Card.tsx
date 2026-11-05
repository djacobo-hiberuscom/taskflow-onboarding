import { Heart, Users, Pen, Archive, Trash } from "lucide-react";
import { Badge } from "#components/ui/badge";
import { Separator } from "#components/ui/separator";
import { useState } from "react";
import TAREAS from "@/assets/tempdata/tasks.json"

import {getUsernameByProjectId } from "@/tools/getUsername";
// Contex menu
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuGroup,
  ContextMenuItem,
  ContextMenuTrigger,
} from "@/components/index.comp";
import type { Project } from "#lib/api/types";

interface parameters {
  proyecto: Project,
  title: string;
  description?: string | null;
  miembros: number;
  tareasAbiertas: number;
  favourite: boolean;
  onOpen: (p: Project) => void,
  onEdit: (p: Project) => void,
  onArchive: (p: Project) => void,
  onDelete: (p: Project) => void,
  isAdmin: boolean
}

export function ProjectCard({
  proyecto,
  title,
  description,
  //miembros,
  favourite,
  onOpen,
  onEdit,
  onDelete,
  onArchive,
  isAdmin
}: parameters) {

  


  const [isFavourite, setIsFavourite] = useState(favourite);
  const miembros = getUsernameByProjectId(proyecto.id).length
  const nTareas = TAREAS.filter((t) => t.project_id === proyecto.id).length; // N TAREAS
  return (
    <ContextMenu>
      <ContextMenuTrigger className="flex flex-col gap-4 p-4 border rounded-xl bg-background max-w-xs hover:brightness-97" onClick={() => onOpen(proyecto) }>
        <div className="flex justify-between">
          <h1 className="font-bold text-xl truncate">{title}</h1>
          <button
            className="self-start shrink-0 bg-background rounded-full p-1 hover:brightness-95 hover:cursor-pointer"
            onClick={(e) => {
              e.stopPropagation();
              setIsFavourite(!isFavourite);
            }}
          >
            <Heart
              className={
                isFavourite ? "fill-red-400 text-red-400" : "text-foreground"
              }
            />
          </button>
        </div>

        <p className="flex-1 line-clamp-3">{description}</p>
        <Separator />
        <div className="flex gap-3 text justify-between items-center">
          <div className="flex gap-2 justify-center items-center">
            <Users />
            <Badge className=" text-[#1E40AF] py-2 bg-[#DBEAFE]">{miembros} miembro{miembros > 1 ? "s": ""}</Badge>
          </div>
          <Badge className="bg-[#DCFCE7] text-[#166534]">{nTareas} tareas</Badge>
        </div>
      </ContextMenuTrigger>
      <ContextMenuContent>
        <ContextMenuGroup className="font-bold">
          <ContextMenuItem disabled={!isAdmin} onClick={() => onEdit(proyecto)}>
            <Pen />
            Edit
          </ContextMenuItem>
          <ContextMenuItem disabled={!isAdmin} onClick={() => onArchive(proyecto)}>
            <Archive />
            Archivar
          </ContextMenuItem>
          <ContextMenuItem disabled={!isAdmin} className="text-red-600" onClick={() => onDelete(proyecto)}>
            <Trash />
            Elimiar
          </ContextMenuItem>
        </ContextMenuGroup>
      </ContextMenuContent>
    </ContextMenu>
  );
}
