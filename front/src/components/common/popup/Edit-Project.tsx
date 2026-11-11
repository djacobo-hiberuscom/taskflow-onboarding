import { Dialog, DialogContent } from "@/components/ui/dialog";
import { useUpdateProject } from "@/hooks/use-project";
import type { Project } from "@/lib/api/types";
import { notify } from "@/lib/notify";
import { ProjectForm } from "./ProjectForm";

interface EditProjectPopUpProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  proyecto?: Project;
}

// Editar nombre y descripción de un proyecto
export function EditProjectPopUp({ isOpen, onOpenChange, proyecto }: EditProjectPopUpProps) {
  const updateProject = useUpdateProject();
  const close = () => onOpenChange(false);

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] flex flex-col p-4 gap-3">
        {proyecto && (
          <ProjectForm
            // key: resetea el formulario al cambiar de proyecto
            key={proyecto.id}
            title="Editar proyecto"
            subtitle="Cambia el nombre y la descripción del proyecto"
            submitLabel="Guardar"
            initial={proyecto}
            isPending={updateProject.isPending}
            onCancel={close}
            onSubmit={(values) =>
              updateProject.mutate(
                { id: proyecto.id, ...values },
                {
                  onSuccess: () => {
                    close();
                    notify.success("Proyecto editado correctamente");
                  },
                  onError: () => notify.error("Error al editar el proyecto"),
                },
              )
            }
          />
        )}
      </DialogContent>
    </Dialog>
  );
}
