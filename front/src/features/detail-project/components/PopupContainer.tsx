// popups
import {
  EditProjectPopUp,
  ArchiveConfirmPopUp,
  DeleteConfirmPopUp,
  NewTaskPopup
} from "@/components/common/popup";

// Types
import type { Project } from "@/lib/api/types";

// NAV
import { useNavigate } from "@tanstack/react-router";

interface params {
  proyecto?: Project;
  popup: string | null;
  close: () => void;
}

// Contenedors
export function PopupContainer({ proyecto, popup, close }: params) {
  const nav = useNavigate();

  return (
    <div>
      <EditProjectPopUp
        isOpen={popup === "Edit"}
        onOpenChange={close}
        proyecto={proyecto}
      />

      <ArchiveConfirmPopUp
        isOpen={popup === "Archive"}
        proyecto={proyecto}
        onOpenChange={close}
        onArchived={() => nav({ to: "/projects", replace: true })}
      />
      <DeleteConfirmPopUp
        isOpen={popup === "Delete"}
        proyecto={proyecto}
        onOpenChange={close}
      />
      <NewTaskPopup 
      isOpen={popup === "Create"}
      onClose={close}
      onOpenChange={close}/>
    </div>
  );
}
