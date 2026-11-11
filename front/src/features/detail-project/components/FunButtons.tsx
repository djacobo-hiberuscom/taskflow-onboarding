import { Archive, Pencil, Trash2 } from "lucide-react";
import { Button } from "@/components";
import { useAuth } from "@/hooks/use-auth";
import type { DetailProjectPopup } from "./types";

// Acciones del proyecto (solo admin)
export function FunButtons({ open }: { open: (popup: DetailProjectPopup) => void }) {
  const { isAdmin } = useAuth();
  return (
    <div className="flex shrink-0 gap-2">
      <Button variant="outline" disabled={!isAdmin} onClick={() => open("edit")}>
        <Pencil />
        Editar
      </Button>
      <Button variant="outline" disabled={!isAdmin} onClick={() => open("archive")}>
        <Archive />
        Archivar
      </Button>
      <Button
        variant="outline"
        disabled={!isAdmin}
        className="border-destructive/30 text-destructive hover:bg-danger-soft hover:text-danger-soft-foreground"
        onClick={() => open("delete")}
      >
        <Trash2 />
        Eliminar
      </Button>
    </div>
  );
}
