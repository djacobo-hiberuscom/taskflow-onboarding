import { Button, Input } from "@/components/index.comp";
import { X } from "lucide-react";
export function NewProjectPopUp() {
  return (
    <div className="flex flex-col gap-4 border rounded-2xl p-4 bg-background max-w-2xl">
      <div id="Header" className="flex justify-between gap-2">
        <div className="flex flex-col gap-3">
          <h1 className="text-2xl font-bold">Nuevo proyecto</h1>
          <p>Crea un proyecto para agrupar las tareas del proyecto</p>
        </div>
        <div
          onClick={() => {}}
          className="p-1 bg-background rounded-full hover:brightness-80 hover:cursor-pointer size-fit"
        >
          <X />
        </div>
      </div>
      <div id="body" className="flex flex-col gap-4">
        <div className="space-y-2">
          <h3>Nombre*</h3>
          <Input placeholder="Nombre" />
        </div>
        <div className="space-y-2">
          <h3>Descripcion</h3>
          <Input placeholder="Descripcion" />
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
          variant="outline"
          className="bg-primary text-background hover:cursor-pointer hover:bg-primary hover:brightness-120 hover:text-background"
        >
          Crear proyecto
        </Button>
      </div>
    </div>
  );
}
