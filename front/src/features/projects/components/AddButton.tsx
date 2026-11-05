// Componentes
import { Button } from "@/components/index.comp";
// Iconos
import { Plus } from "lucide-react";

// parametros
interface parameters {
  onClic: () => void;
}

// compoenente
export function AddButton({ onClic }: parameters) {
  return (
    <Button
      className="rounded-full min-w-12 min-h-12 bg-primary hover:cursor-pointer hover:bg-primary hover:brightness-120"
      variant="outline"
      onClick={onClic}
    >
      <Plus className="size-7 text-white" />
    </Button>
  );
}
