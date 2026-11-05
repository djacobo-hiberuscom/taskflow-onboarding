// Popups
import type { Project } from "#lib/api/types";
import {
  ArchiveConfirmPopUp,
  ArchivedPopUp,
  DeleteConfirmPopUp,
  EditProjectPopUp,
  NewProjectPopup,
} from "@/components/common/popup";

// hooks
import { useNavigate } from "@tanstack/react-router";

// Parameters
interface params {
  popup: string | null;
  close: (open: boolean) => void;
  proyecto: Project | undefined;
}
// contenedor
export function PopupContainer({ popup, close, proyecto }: params) {
    const nav = useNavigate()

  return (
    <div>
      <ArchivedPopUp isOpen={popup === "Archive"} onOpenChange={close} />
      <NewProjectPopup isOpen={popup === "New"} onOpenChange={close} />

      <DeleteConfirmPopUp
        isOpen={popup === "Delete"}
        proyecto={proyecto}
        onOpenChange={close}
      />
      <ArchiveConfirmPopUp
        isOpen={popup === "ArchiveConfirm"}
        proyecto={proyecto}
        onOpenChange={close}
        onArchived={() => nav({ to: "/projects", replace: true })}
      />
      <EditProjectPopUp
        isOpen={popup === "Edit"}
        onOpenChange={close}
        proyecto={proyecto}
      />
    </div>
  );
}
