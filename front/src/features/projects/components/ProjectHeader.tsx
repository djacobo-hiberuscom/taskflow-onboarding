import { Archive, Plus } from "lucide-react";
import { Button } from "@/components";
import { useAuth } from "@/hooks/use-auth";

interface ProjectsHeaderProps {
  archivedCount?: number;
  openArchived: () => void;
  openNewProject: () => void;
}

// Cabecera: título + ver archivados (admin) + crear proyecto
export function ProjectsHeader({ archivedCount, openArchived, openNewProject }: ProjectsHeaderProps) {
  const { isAdmin } = useAuth();

  return (
    <header className="flex flex-wrap items-start justify-between gap-4">
      <div>
        <h1 className="text-2xl font-bold">Proyectos</h1>
        <p className="text-sm text-muted-foreground">Gestiona y organiza todos tus proyectos.</p>
      </div>

      <div className="flex gap-2">
        <Button variant="outline" size="lg" disabled={!isAdmin} onClick={openArchived}>
          <Archive />
          Archivados
          {archivedCount !== undefined && (
            <span className="rounded-full bg-muted px-2 text-xs">{archivedCount}</span>
          )}
        </Button>
        <Button size="lg" onClick={openNewProject}>
          <Plus />
          Crear proyecto
        </Button>
      </div>
    </header>
  );
}
