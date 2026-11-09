// Compoenents
import { Separator, Button } from "@/components/index.comp";

// icons
import {Pen, Archive, Trash,} from "lucide-react"

// Parametros
interface params {
    editClic: () => void,
    archiveClic: () => void,
    deleteClic: () => void
}


// compoenente
export function FunButtons({editClic, archiveClic, deleteClic}: params) {
  return (
    <div className="flex gap-2 h-fit">
      <Button variant="outline" onClick={editClic}>
        <Pen />
        Editar
      </Button>
      <Button variant="outline" onClick={archiveClic}>
        <Archive />
        Archivar
      </Button>
      <Separator orientation="vertical" />

      <Button
        variant="outline"
        className="text-red-400 hover:text-red-400"
        onClick={deleteClic}
      >
        <Trash />
        Eliminar
      </Button>
    </div>
  );
}
