import type { Project } from "#lib/api/types";
// COMPONENTES
import { Dialog, DialogContent, Button, Input } from "@/components/index.comp";

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
  if (!proyecto) return <h1>Error al obtener proyecto</h1>;
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
          <Input placeholder="Nombre" value={proyecto.name} />
        </div>
        <div className="space-y-2">
          <h3>Descripcion</h3>
          <Input placeholder="Descripcion" value={proyecto.description ?? "Sin Descripcion"} />
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
          variant="outline"
          className="bg-primary text-background hover:cursor-pointer hover:bg-primary hover:brightness-120 hover:text-background"
        >
          Guardar
        </Button>
      </div>
    </div>
  );
}
