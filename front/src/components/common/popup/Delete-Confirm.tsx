import { Trash, TriangleAlertIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent } from "@/components/ui/dialog";

interface ConfirmDeletePopUpProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description: string;
  warning?: string;
  onConfirm: () => void;
  isPending?: boolean;
  modal?: boolean;
}

// Diálogo genérico de "¿Seguro que quieres eliminar...?"
export function ConfirmDeletePopUp({
  isOpen,
  onOpenChange,
  title,
  description,
  warning,
  onConfirm,
  isPending = false,
  modal,
}: ConfirmDeletePopUpProps) {
  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange} modal={modal}>
      <DialogContent className="sm:max-w-xl max-h-[90vh] flex flex-col p-4 gap-3">
        <div className="flex gap-2">
          <div className="p-1">
            <Trash />
          </div>
          <div className="flex flex-col gap-1">
            <h1 className="text-xl">{title}</h1>
            <p>{description}</p>
          </div>
        </div>

        {warning && (
          <div className="flex gap-2 bg-danger-soft rounded-xl p-3 text-danger-soft-foreground items-center">
            <TriangleAlertIcon />
            <p>{warning}</p>
          </div>
        )}

        <div className="flex gap-1 justify-end">
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancelar
          </Button>
          <Button variant="destructive" disabled={isPending} onClick={onConfirm}>
            {isPending ? "Eliminando..." : "Eliminar"}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
