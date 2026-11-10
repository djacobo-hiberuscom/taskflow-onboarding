// componentes
import {Button} from "@/components/index.comp"

// BOTONOERA
interface botonParameters {
  nextClic: () => void;
  previousClic: () => void;
  actualPage: number;
  totalPages: number;
}
export function BotoneraFooter({
  nextClic,
  previousClic,
  actualPage,
  totalPages,
}: botonParameters) {
  return (
    <div className="sticky bottom-5 mt-auto flex justify-end items-center gap-2">
      <Button onClick={previousClic} variant="outline">
        Anterior
      </Button>
      <span>
        {actualPage} / {totalPages}
      </span>
      <Button onClick={nextClic} variant="outline">
        Siguiente
      </Button>
    </div>
  );
}