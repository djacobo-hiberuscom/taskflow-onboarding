// DIALOG import

import { Dialog, DialogContent, Button } from "@/components/index.comp";

// COMPONENTS
import { toast } from "@/components/index.comp";
// TYPES
import type { Project } from "#lib/api/types";

// ICON
import { Trash, TriangleAlertIcon } from "lucide-react";

// HOOKS
import { useDeleteProject } from "#hooks/use-project";
import { useNavigate } from "@tanstack/react-router";

const openSuccessToast = () => {
  toast.add({
    type: "success",
    description: "Proyecto eliminado correctamente",
  });
};
const openErrorToast = () => {
  toast.add({
    type: "error",
    description: "Error al eliminar el proyecto",
  });
};

interface popParameters {
  modal?: boolean;
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  proyecto?: Project;
}

export function DeleteConfirmPopUp({
  modal,
  isOpen,
  onOpenChange,
  proyecto,
}: popParameters) {
  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange} modal={modal}>
      <DialogContent className="sm:max-w-xl max-h-[90vh] flex flex-col p-4 gap-3">
        <DeleteConfirmContainer
          proyecto={proyecto}
          onClose={() => onOpenChange(false)}
        />
      </DialogContent>
    </Dialog>
  );
}

interface parameters {
  proyecto?: Project;
  onClose: () => void;
}
export function DeleteConfirmContainer({ proyecto, onClose }: parameters) {
  const nav = useNavigate();
  const deleteProject = useDeleteProject();

  const handleClicDelete = () => {
    if (!proyecto) return;
    deleteProject.mutate(proyecto.id, {
      onSuccess: () => {
        onClose();
        nav({ to: "/projects", replace: true });
        openSuccessToast()
      },
      onError: () => {
        onClose()
        openErrorToast()
      }
    });
  };

  return (
    <div className="flex flex-col gap-2">
      <div className="flex flex-col gap-2 ">
        <div className="flex gap-2">
          <div className="p-1">
            <Trash />
          </div>
          <div className="flex flex-col gap-1">
            <h1 className="text-xl">¿Eliminar: {proyecto?.name}?</h1>
            <p>El proyecto se borrara de forma permananente</p>
          </div>
        </div>
        <div className="flex gap-2 bg-red-200 rounded-xl p-3 border-red-800 text-red-900 items-center">
          <TriangleAlertIcon />
          <p>
            Se eliminar las tareas asociadas al proyecto. Esta accion no se
            puede deshacer
          </p>
        </div>
      </div>
      <div className="flex gap-1 justify-end">
        <Button variant="outline" onClick={onClose}>
          Cancelar
        </Button>
        <Button
          variant="outline"
          className="bg-red-500 text-white"
          onClick={handleClicDelete}
        >
          Eliminar
        </Button>
      </div>
    </div>
  );
}
