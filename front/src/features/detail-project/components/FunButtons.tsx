import { Archive, Pen, Plus, Trash } from "lucide-react";
import { Button, Separator } from "@/components/index.comp";
import { useAuth } from "@/hooks/use-auth";
import type { DetailProjectPopup } from "./types";

// Acciones del proyecto
export function FunButtons({ open }: { open: (popup: DetailProjectPopup) => void }) {
  const { isAdmin } = useAuth();
  return (
    <div className="flex flex-col gap-2 h-fit">
      <div className="flex gap-1">
        <Button variant="outline" disabled={!isAdmin} onClick={() => open("edit")}>
          <Pen />
          Editar
        </Button>
        <Button variant="outline" disabled={!isAdmin} onClick={() => open("archive")}>
          <Archive />
          Archivar
        </Button>
        <Separator orientation="vertical" />
        <Button
          variant="outline"
          disabled={!isAdmin}
          className="text-red-400 hover:text-red-400"
          onClick={() => open("delete")}
        >
          <Trash />
          Eliminar
        </Button>
      </div>

      <Button onClick={() => open("create-task")}>
        <Plus />
        Crear tarea
      </Button>
    </div>
  );
}
