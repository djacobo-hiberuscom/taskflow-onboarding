import {
  DialogContent,
  Dialog,
  toast,
  Input,
  Button,
  FilterSeletor,
} from "@/components/index.comp";
import { useState } from "react";

// Toast
const openSuccessToast = () => {
  toast.add({
    type: "success",
    description: "Tarea creada correctamente",
  });
};
const openErrorToast = () => {
  toast.add({
    type: "error",
    description: "Error al crear el proyecto",
  });
};

function onHandleClicSave() {}

// parametros del componente
interface params {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  onClose: () => void;
}
//popup
export function NewTaskPopup({ isOpen, onOpenChange, onClose }: params) {
  // states
  const [nombre, setNombre] = useState("");
  const [descripcion, setDescripcion] = useState("");
  const [miembro, setMiembro] = useState<string | null>(null);
  const [estatus, setEstatus] = useState<string | null>(null);
  const [prioridad, setPrioridad] = useState<string | null>(null);

  return (
    <div>
      <Dialog open={isOpen} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-2xl">
          <div className="flex flex-col gap-2">
            <h1>Crear una nueva tarea</h1>
            <span>Nombre</span>
            <Input
              placeholder="Nombre"
              onChange={(e) => setNombre(e.target.value)}
              value={nombre}
            />
            <span>Descripcion</span>
            <Input
              placeholder="Descripcion"
              onChange={(e) => setDescripcion(e.target.value)}
              value={descripcion}
            />
            <div className="flex justify-between">
              <div>
                <span>Miembro</span>
                <FilterSeletor
                  items={[]}
                  placeholder="Asignar"
                  value={miembro}
                  onChange={(e) => setMiembro(e)}
                />
              </div>
              <div>
                <span>Estatus</span>
                <FilterSeletor
                  items={[]}
                  placeholder="Estado"
                  value={estatus}
                  onChange={(e) => setEstatus(e)}
                />
              </div>
              <div>
                <span>Prioridad</span>
                <FilterSeletor
                  items={[]}
                  placeholder="Prioridad"
                  value={prioridad}
                  onChange={(e) => setPrioridad(e)}
                />
              </div>
            </div>
            <div className="flex gap-2 justify-end">
              <Button
                variant="outline"
                className="text-red-500 hover:brightness-98 hover:text-red-500"
                onClick={onClose}
              >
                Cancelar
              </Button>
              <Button onClick={onHandleClicSave}>Guardar</Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
