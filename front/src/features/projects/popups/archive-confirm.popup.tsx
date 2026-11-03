// DIALOG import

import { Dialog, DialogContent, Button } from "@/components/index.comp";

// COMPONENTS

// TYPES
import type { Project } from "#lib/api/types";

// ICON
import { Archive } from "lucide-react";

interface popParameters {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  proyecto?: Project;
}

export function ArchiveConfirmPopUp({
  isOpen,
  onOpenChange,
  proyecto,
}: popParameters) {
  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-xl max-h-[90vh] flex flex-col p-4 gap-3">
        <ArchiveConfirmContainer proyecto={proyecto} />
      </DialogContent>
    </Dialog>
  );
}

interface parameters {
  proyecto?: Project;
}
export function ArchiveConfirmContainer({ proyecto }: parameters) {
  return (
    <div className="flex flex-col gap-2">
      <div className="flex flex-row gap-2 ">
        <div className="p-1">
          <Archive/>
        </div>
        <div>
          <h1 className="text-xl">¿Archivar: {proyecto?.name}?</h1>
          <p>
            El proyecto dejara de ser vidible para los usuario, solo el admin
            podra restaurarlo
          </p>
        </div>
      </div>
      <div className="flex gap-1 justify-end">
        <Button variant="outline">Cancelar</Button>
        <Button variant="outline" className="bg-blue-500 text-white">
          Archivar
        </Button>
      </div>
    </div>
  );
}
