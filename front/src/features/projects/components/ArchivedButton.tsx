// Compoenentees
import { Button } from "@/components/index.comp";

// Iconos
import { Archive } from "lucide-react";

// parametros
interface parameters {
  onClic: () => void;
  isAdmin: boolean
}

// Compoenente
export function ArchivedButton({ onClic, isAdmin}: parameters) {
  return (
    <Button
      disabled={!isAdmin}
      className="rounded-full min-w-12 min-h-12 bg-background hover:cursor-pointer hover:brightness-90"
      variant="outline"
      onClick={onClic}
    >
      <Archive className="size-7" />
    </Button>
  );
}
