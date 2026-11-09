// COMPONENTES
import {
  Dialog,
  DialogContent,
  Button,
  Input,
  toast,
} from "@/components/index.comp";
import { useState } from "react";
// TYPES
import { useCreateProject } from "@/hooks/use-project";

const openSuccessToast = () => {
  toast.add({
    type: "success",
    description: "Proyecto creado correctamente",
  });
};
const openErrorToast = () => {
  toast.add({
    type: "error",
    description: "Error al crear el proyecto",
  });
};

interface parameters {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
}
export function NewProjectPopup({ isOpen, onOpenChange }: parameters) {
  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] flex flex-col p-4 gap-3">
        <NewProjectContainer onClose={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  );
}

function NewProjectContainer({ onClose }: { onClose: () => void }) {
  const [nombre, setNombre] = useState<string>("");
  const [description, setDescription] = useState<string>("");

  const createProject = useCreateProject();
  const handleCreateClic = () => {
    if (nombre.trim().length === 0) return;
    createProject.mutate(
      { name: nombre.trim(), description: description.trim() || null },
      {
        onSuccess: () => {
          setNombre("");
          setDescription("");
          onClose();
          openSuccessToast();
        },
        onError: () => {
          onClose();
          openErrorToast();
        },
      },
    );
  };

  return (
    <div className="flex flex-col gap-4 rounded-2xl p-4 max-w-2xl">
      <div id="Header" className="flex justify-between gap-2">
        <div className="flex flex-col gap-3">
          <h1 className="text-2xl font-bold">Nuevo proyecto</h1>
          <p>Crea un proyecto para agrupar las tareas del proyecto</p>
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
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </div>
      </div>
      <div id="botonera" className="flex justify-end gap-3">
        <Button
          variant="outline"
          className="bg-background hover:cursor-pointer"
        >
          Cancelar
        </Button>
        <Button
          onClick={handleCreateClic}
          variant="outline"
          className="bg-primary text-background hover:cursor-pointer hover:bg-primary hover:brightness-120 hover:text-background"
        >
          Crear proyecto
        </Button>
      </div>
    </div>
  );
}
