import { Archive, Plus } from "lucide-react";
import { Button } from "@/components/index.comp";
import { useAuth } from "@/hooks/use-auth";

interface BotoneraProps {
  openArchived: () => void;
  openNewProject: () => void;
}

// Botones flotantes: ver archivados (admin) y crear proyecto
export function Botonera({ openArchived, openNewProject }: BotoneraProps) {
  const { isAdmin } = useAuth();
  return (
    <div className="sticky bottom-4 mt-auto flex justify-end gap-2">
      <Button
        variant="outline"
        disabled={!isAdmin}
        className="rounded-full min-w-12 min-h-12 bg-background hover:brightness-90"
        onClick={openArchived}
        aria-label="Ver proyectos archivados"
      >
        <Archive className="size-7" />
      </Button>
      <Button
        className="rounded-full min-w-12 min-h-12 hover:brightness-120"
        onClick={openNewProject}
        aria-label="Nuevo proyecto"
      >
        <Plus className="size-7 text-white" />
      </Button>
    </div>
  );
}
