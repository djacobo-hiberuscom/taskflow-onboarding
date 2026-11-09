import { useUpdateProject } from "@/hooks/use-project";
import type { Project } from "@/lib/api/types";
// COMPONENTES
import { Dialog, DialogContent, Button, Input } from "@/components/index.comp";
import { useState } from "react";
import { toast } from "@/components/index.comp";

interface popParameters {
  isOpen: boolean;
  onOpenChange: (opne: boolean) => void;
  proyecto?: Project | null;
}
export function EditProjectPopUp({
  isOpen,
  onOpenChange,
  proyecto,
}: popParameters) {
  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] flex flex-col p-4 gap-3">
        <EditProjectContainer
          proyecto={proyecto}
          onClose={() => onOpenChange(false)}
        />
      </DialogContent>
    </Dialog>
  );
}

interface parameters {
  proyecto?: Project | null;
  onClose: () => void;
}
function EditProjectContainer({ proyecto, onClose }: parameters) {
  const [nombre, setNombre] = useState(proyecto?.name);
  const [description, setDescription] = useState(proyecto?.description);
  const useUpdate = useUpdateProject();

  if (!proyecto) return <h1>Error al obtener proyecto</h1>;

  const openSuccessToast = () => {
    toast.add({
      type: "success",
      description: "Proyecto editado correctamente",
    });
  };
  const openErrorToast = () => {
    toast.add({
      type: "error",
      description: "Error al editar el proyecto",
    });
  }

  const onUpdateHandleClic = () => {
    useUpdate.mutate(
      {
        id: proyecto.id,
        name: nombre ?? "Sin nombre",
        description: description?.trim() || null,
      },
      {
        onSuccess: () => {
          onClose();
          openSuccessToast()
        },
        onError: () => {
          onClose()
          openErrorToast()
        }
      },
    );
  };

  return (
    <div className="flex flex-col gap-4 rounded-2xl p-4 max-w-2xl">
      <div id="Header" className="flex justify-between gap-2">
        <div className="flex flex-col gap-3">
          <h1 className="text-2xl font-bold">Editar proyecto</h1>
          <p>Cambia el nombre y la descripcion del proyecto</p>
        </div>
        <div
          onClick={() => {}}
          className="p-1 bg-background rounded-full hover:brightness-80 hover:cursor-pointer size-fit"
        ></div>
      </div>
      <div id="body" className="flex flex-col gap-4">
        <div className="space-y-2">
          <h3>Nombre*</h3>
          <Input
            placeholder="Nombre"
            value={nombre}
            onChange={(e) => setNombre(e.target.value)}
          />
        </div>
        <div className="space-y-2">
          <h3>Descripcion</h3>
          <Input
            placeholder="Descripcion"
            value={description ?? ""}
            onChange={(e) => setDescription(e.target.value)}
          />
        </div>
      </div>
      <div id="botonera" className="flex justify-end gap-3">
        <Button
          onClick={onClose}
          variant="outline"
          className="bg-background hover:cursor-pointer"
        >
          Cancelar
        </Button>
        <Button
          onClick={onUpdateHandleClic}
          variant="outline"
          className="bg-primary text-background hover:cursor-pointer hover:bg-primary hover:brightness-120 hover:text-background"
        >
          Guardar
        </Button>
      </div>
    </div>
  );
}
