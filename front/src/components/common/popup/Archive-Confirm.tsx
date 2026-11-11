import { Archive } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent } from "@/components/ui/dialog";
import { useArchiveProject } from "@/hooks/use-project";
import type { Project } from "@/lib/api/types";
import { notify } from "@/lib/notify";

interface ArchiveConfirmPopUpProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  proyecto?: Project;
  onArchived?: () => void;
}

// Confirmar y archivar un proyecto
export function ArchiveConfirmPopUp({
  isOpen,
  onOpenChange,
  proyecto,
  onArchived,
}: ArchiveConfirmPopUpProps) {
  const archiveProject = useArchiveProject();

  const handleArchive = () => {
    if (!proyecto) return;
    archiveProject.mutate(proyecto.id, {
      onSuccess: () => {
        onOpenChange(false);
        onArchived?.();
        notify.success("Proyecto archivado correctamente");
      },
      onError: () => {
        onOpenChange(false);
        notify.error("Error al archivar el proyecto");
      },
    });
  };

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-xl max-h-[90vh] flex flex-col p-4 gap-3">
        <div className="flex flex-row gap-2">
          <div className="p-1">
            <Archive />
          </div>
          <div>
            <h1 className="text-xl">¿Archivar: {proyecto?.name}?</h1>
            <p>
              El proyecto dejará de ser visible para los usuarios, solo el admin podrá
              restaurarlo
            </p>
          </div>
        </div>
        <div className="flex gap-1 justify-end">
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancelar
          </Button>
          <Button
            disabled={!proyecto || archiveProject.isPending}
            onClick={handleArchive}
          >
            {archiveProject.isPending ? "Archivando..." : "Archivar"}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
