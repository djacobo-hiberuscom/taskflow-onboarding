import { Archive, FolderPlus, Plus } from "lucide-react";
import { Button } from "@/components";
import { useAuth } from "@/hooks/use-auth";

interface ProjectsEmptyStateProps {
  onCreate: () => void;
  onOpenArchived: () => void;
}

// Estado vacío: no hay proyectos activos
export function ProjectsEmptyState({ onCreate, onOpenArchived }: ProjectsEmptyStateProps) {
  const { isAdmin } = useAuth();

  return (
    <div className="flex flex-col items-center justify-center gap-2 rounded-xl border bg-card px-4 py-16 text-center">
      <div className="mb-3 grid size-22 place-items-center rounded-full border bg-primary/5">
        <FolderPlus className="size-5 text-primary" />
      </div>

      <h2 className="font-semibold">no tienes proyectos</h2>
      <p className="max-w-xs text-sm text-muted-foreground">
        Crea tu primer proyecto para organizar tareas, miembros y plazos en un solo lugar.
      </p>

      <div className="mt-3 flex flex-wrap justify-center gap-2">
        <Button size="lg" onClick={onCreate}>
          <Plus />
          Crear proyecto
        </Button>
        <Button variant="outline" size="lg" disabled={!isAdmin} onClick={onOpenArchived}>
          <Archive />
          Ver archivados
        </Button>
      </div>
    </div>
  );
}
