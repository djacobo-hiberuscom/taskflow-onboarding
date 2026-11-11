import { Dialog, DialogContent } from "@/components/ui/dialog";
import { useCreateProject } from "@/hooks/use-project";
import { notify } from "@/lib/notify";
import { ProjectForm } from "./ProjectForm";

interface NewProjectPopupProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
}

// Crear proyecto
export function NewProjectPopup({ isOpen, onOpenChange }: NewProjectPopupProps) {
  const createProject = useCreateProject();
  const close = () => onOpenChange(false);

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] flex flex-col p-4 gap-3">
        <ProjectForm
          title="Nuevo proyecto"
          subtitle="Crea un proyecto para agrupar sus tareas"
          submitLabel="Crear proyecto"
          isPending={createProject.isPending}
          onCancel={close}
          onSubmit={(values) =>
            createProject.mutate(values, {
              onSuccess: () => {
                close();
                notify.success("Proyecto creado correctamente");
              },
              onError: () => notify.error("Error al crear el proyecto"),
            })
          }
        />
      </DialogContent>
    </Dialog>
  );
}
