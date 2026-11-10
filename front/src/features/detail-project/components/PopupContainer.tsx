// popups
import { useDeleteProject } from "#hooks/use-project";
import {
  EditProjectPopUp,
  ArchiveConfirmPopUp,
  DeleteConfirmPopUp,
  NewTaskPopup,
} from "@/components/common/popup";

// COMponents
import {toast} from "@/components/index.comp"

// Types
import type { Project } from "@/lib/api/types";

// NAV
import { useNavigate } from "@tanstack/react-router";

interface params {
  proyecto: Project;
  popup: string | null;
  close: () => void;
}

// Contenedors
export function PopupContainer({ proyecto, popup, close }: params) {
  const nav = useNavigate();
  // DELETE
  const deleteProject = useDeleteProject();

  const handleClicDeleteProject = () => {
    if (!proyecto) return;
    deleteProject.mutate(proyecto.id, {
      onSuccess: () => {
        close()
        nav({ to: "/projects", replace: true });
        openToast("Se ha borrado el proyecto correctamente", "success")
      },
      onError: () => {
        close()
        openToast("Error al borrar el proyecto", "error")
      }
    });
  };



  return (
    <div>
      <EditProjectPopUp
        isOpen={popup === "Edit"}
        onOpenChange={close}
        proyecto={proyecto}
      />

      <ArchiveConfirmPopUp
        isOpen={popup === "Archive"}
        proyecto={proyecto}
        onOpenChange={close}
        onArchived={() => nav({ to: "/projects", replace: true })}
      />
      <DeleteConfirmPopUp
        onClic={handleClicDeleteProject}
        isOpen={popup === "Delete"}
        proyecto={proyecto}
        onOpenChange={close}
      />
      <NewTaskPopup
        proyecto={proyecto}
        isOpen={popup === "Create"}
        onClose={close}
        onOpenChange={close}
      />
    </div>
  );
}


// Abrir un toast
const openToast = (message: string, type: string) => {
  toast.add({
    type: type,
    description: message,
  });
};
