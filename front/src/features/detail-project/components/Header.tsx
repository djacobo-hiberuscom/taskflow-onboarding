import { FolderKanban, ListChecks } from "lucide-react";
import { useTaskCount } from "@/hooks/use-task";
import type { ProjectDetail } from "@/lib/api/types";
import { FunButtons } from "./FunButtons";
import { MemberAvatars } from "./MemberAvatars";
import type { DetailProjectPopup } from "./types";

interface HeaderProps {
  proyecto: ProjectDetail;
  open: (popup: DetailProjectPopup) => void;
}

// Cabecera: icono, nombre, descricion, métricas y acciones
export function Header({ proyecto, open }: HeaderProps) {
  const { data: total = 0 } = useTaskCount({ projectId: proyecto.id });

  return (
    <header className="flex w-full flex-col gap-4 border-b pb-6 md:flex-row md:items-start md:justify-between">
      <div className="flex min-w-0 gap-4">
        <div className="flex size-12 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
          <FolderKanban className="size-6" />
        </div>

        <div className="flex min-w-0 flex-col gap-2">
          <h1 className="text-2xl font-bold tracking-tight">{proyecto.name}</h1>
          {proyecto.description && (
            <p className="max-w-2xl text-sm text-muted-foreground">{proyecto.description}</p>
          )}

          <div className="mt-1 flex flex-wrap items-center gap-x-5 gap-y-2 text-xs text-muted-foreground">
            <span className="flex items-center gap-1.5">
              <ListChecks className="size-4" />
              <strong className="font-semibold text-foreground">{total}</strong> tareas
            </span>

            <MemberAvatars miembros={proyecto.members} />
          </div>
        </div>
      </div>

      <FunButtons open={open} />
    </header>
  );
}
