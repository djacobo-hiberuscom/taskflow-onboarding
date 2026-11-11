import { useState } from "react";
import { Archive, FolderKanban, ListChecks, Pen, Star, Trash, Users } from "lucide-react";
import {
  Avatar,
  AvatarFallback,
  AvatarGroup,
  AvatarGroupCount,
  ContextMenu,
  ContextMenuContent,
  ContextMenuGroup,
  ContextMenuItem,
  ContextMenuTrigger,
  Separator,
} from "@/components";
import { cn } from "cn";
import { useProject } from "@/hooks/use-project";
import { useTaskCount } from "@/hooks/use-task";
import type { Project, UserSummary } from "@/lib/api/types";

// Color estable por id (el mismo proyecto/usuario -> siempre el mismo color)
const TONES = [
  "bg-tone-1 text-tone-1-foreground",
  "bg-tone-2 text-tone-2-foreground",
  "bg-tone-3 text-tone-3-foreground",
  "bg-tone-4 text-tone-4-foreground",
  "bg-tone-5 text-tone-5-foreground",
  "bg-tone-6 text-tone-6-foreground",
];
function toneFor(id: string) {
  let h = 0;
  for (const c of id) h = (h * 31 + c.charCodeAt(0)) >>> 0;
  return TONES[h % TONES.length];
}

const initials = (name: string) =>
  name
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((w) => w[0]!.toUpperCase())
    .join("");

// Avatares apilados con "+N" si hay más de `max`
function MembersAvatars({ members, max = 4 }: { members: UserSummary[]; max?: number }) {
  const extra = members.length - max;
  return (
    <AvatarGroup>
      {members.slice(0, max).map((m) => (
        <Avatar key={m.id} size="sm" title={m.displayName}>
          <AvatarFallback className={cn("text-[10px] font-semibold", toneFor(m.id))}>
            {initials(m.displayName)}
          </AvatarFallback>
        </Avatar>
      ))}
      {extra > 0 && <AvatarGroupCount className="size-6 text-[10px]">+{extra}</AvatarGroupCount>}
    </AvatarGroup>
  );
}

interface ProjectCardProps {
  proyecto: Project;
  isAdmin: boolean;
  onOpen: (p: Project) => void;
  onEdit: (p: Project) => void;
  onArchive: (p: Project) => void;
  onDelete: (p: Project) => void;
}

// Tarjeta de proyecto (clic = abrir, clic derecho = acciones de admin)
export function ProjectCard({ proyecto, isAdmin, onOpen, onEdit, onArchive, onDelete }: ProjectCardProps) {
  // Estrella solo visual: no hay favoritos en el back
  const [starred, setStarred] = useState(false);

  // El listado no trae miembros -> se piden al detalle (queda cacheado para la vista de detalle)
  const { data: detail } = useProject(proyecto.id);
  const { data: nTareas = 0 } = useTaskCount({ projectId: proyecto.id });
  const members = detail?.members ?? [];

  return (
    <ContextMenu>
      <ContextMenuTrigger
        className="flex cursor-pointer flex-col gap-3 rounded-xl border bg-card p-5 transition-shadow hover:shadow-md"
        onClick={() => onOpen(proyecto)}
      >
        <div className="flex items-start justify-between">
          <div className={cn("grid size-10 place-items-center rounded-lg", toneFor(proyecto.id))}>
            <FolderKanban className="size-5" />
          </div>
          <button
            aria-label="Destacar proyecto"
            aria-pressed={starred}
            className="rounded-md p-1 hover:bg-muted"
            onClick={(e) => {
              e.stopPropagation();
              setStarred((s) => !s);
            }}
          >
            <Star
              className={cn(
                "size-5",
                starred ? "fill-favorite text-favorite" : "text-muted-foreground",
              )}
            />
          </button>
        </div>

        <h3 className="truncate font-semibold">{proyecto.name}</h3>
        <p className="min-h-10 text-sm text-muted-foreground line-clamp-2">{proyecto.description}</p>

        <Separator className="mt-auto" />

        <div className="flex items-center justify-between gap-2 text-xs text-muted-foreground">
          <MembersAvatars members={members} />
          <div className="flex items-center gap-3">
            <span className="flex items-center gap-1">
              <Users className="size-3.5" />
              {members.length} miembro{members.length === 1 ? "" : "s"}
            </span>
            <span className="flex items-center gap-1">
              <ListChecks className="size-3.5" />
              {nTareas} tareas
            </span>
          </div>
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
          <ContextMenuItem disabled={!isAdmin} variant="destructive" onClick={() => onDelete(proyecto)}>
            <Trash />
            Eliminar
          </ContextMenuItem>
        </ContextMenuGroup>
      </ContextMenuContent>
    </ContextMenu>
  );
}
