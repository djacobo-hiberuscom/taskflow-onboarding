// DIALOG import

import { Dialog, DialogContent, Button } from "@/components/index.comp";

// COMPONENTS
import { toast } from "@/components/index.comp";
// TYPES
import type { Project } from "#lib/api/types";

// ICON
import { Archive } from "lucide-react";

// HOOKS
import { useArchiveProject } from "@/hooks/use-project";

const openSuccessToast = () => {
  toast.add({
    type: "success",
    description: "Proyecto archivado correctamente",
  });
};
const openErrorToast = () => {
  toast.add({
    type: "error",
    description: "Error al archivar el proyecto",
  });
};

interface popParameters {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  proyecto?: Project;
  onArchived: () => void;
}

export function ArchiveConfirmPopUp({
  isOpen,
  onOpenChange,
  proyecto,
  onArchived,
}: popParameters) {
  return (
    <div>
      <Dialog open={isOpen} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-xl max-h-[90vh] flex flex-col p-4 gap-3">
          <ArchiveConfirmContainer
            proyecto={proyecto}
            onClose={() => onOpenChange(false)}
            onArchived={onArchived}
          />
        </DialogContent>
      </Dialog>
    </div>
  );
}

interface parameters {
  proyecto?: Project;
  onClose: () => void;
  onArchived?: () => void;
}
export function ArchiveConfirmContainer({
  proyecto,
  onClose,
  onArchived,
}: parameters) {
  const archiveProject = useArchiveProject();

  const handleClicArchive = () => {
    if (!proyecto) return;
    archiveProject.mutate(proyecto.id, {
      onSuccess: () => {
        onClose();
        onArchived?.();
        openSuccessToast();
      },
      onError: () => {
        onClose();
        openErrorToast();
      },
    });
  };

  return (
    <div className="flex flex-col gap-2">
      <div className="flex flex-row gap-2 ">
        <div className="p-1">
          <Archive />
        </div>
        <div>
          <h1 className="text-xl">¿Archivar: {proyecto?.name}?</h1>
          <p>
            El proyecto dejara de ser vidible para los usuario, solo el admin
            podra restaurarlo
          </p>
        </div>
      </div>
      <div className="flex gap-1 justify-end">
        <Button variant="outline" onClick={onClose}>
          Cancelar
        </Button>
        <Button
          variant="outline"
          className="bg-blue-500 text-white"
          disabled={!proyecto || archiveProject.isPending}
          onClick={handleClicArchive}
        >
          {archiveProject.isPending ? "Archivando..." : "Archivar"}
        </Button>
      </div>
    </div>
  );
}
