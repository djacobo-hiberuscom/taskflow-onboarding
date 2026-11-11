import { useDeleteProject } from "@/hooks/use-project";
import type { Project } from "@/lib/api/types";
import { notify } from "@/lib/notify";
import { ConfirmDeletePopUp } from "./Delete-Confirm";

interface DeleteProjectPopUpProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  proyecto?: Project;
  onDeleted?: () => void;
}

// Confirmar y eliminar un proyecto
export function DeleteProjectPopUp({
  isOpen,
  onOpenChange,
  proyecto,
  onDeleted,
}: DeleteProjectPopUpProps) {
  const deleteProject = useDeleteProject();

  const handleConfirm = () => {
    if (!proyecto) return;
    deleteProject.mutate(proyecto.id, {
      onSuccess: () => {
        onOpenChange(false);
        onDeleted?.();
        notify.success("Se ha borrado el proyecto correctamente");
      },
      onError: () => {
        onOpenChange(false);
        notify.error("Error al borrar el proyecto");
      },
    });
  };

  return (
    <ConfirmDeletePopUp
      isOpen={isOpen}
      onOpenChange={onOpenChange}
      title={`¿Eliminar: ${proyecto?.name ?? ""}?`}
      description="El proyecto se borrará de forma permanente"
      warning="Se eliminarán las tareas asociadas al proyecto. Esta acción no se puede deshacer"
      onConfirm={handleConfirm}
      isPending={deleteProject.isPending}
    />
  );
}
