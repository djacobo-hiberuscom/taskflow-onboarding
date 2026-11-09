// Compoenents
import { Separator, Button } from "@/components/index.comp";

// icons
import { Pen, Archive, Trash, Plus } from "lucide-react";

// Parametros
interface params {
  editClic: () => void;
  archiveClic: () => void;
  deleteClic: () => void;
  createClic: () => void;
}

// compoenente
export function FunButtons({
  editClic,
  archiveClic,
  deleteClic,
  createClic,
}: params) {
  return (
    <div className="flex flex-col gap-2 h-fit">
      <div className="flex gap-1">
        <Button variant="outline" onClick={editClic}>
          <Pen />
          Editar
        </Button>

        <Button variant="outline" onClick={archiveClic}>
          <Archive />
          Archivar
        </Button>
    <Separator orientation="vertical"/>
        <Button
          variant="outline"
          className="text-red-400 hover:text-red-400"
          onClick={deleteClic}
        >
          <Trash />
          Eliminar
        </Button>
      </div>
      
      <Button onClick={createClic}>
        <Plus />
        Crear tarea
      </Button>
    </div>
  );
}
