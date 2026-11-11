import { useNavigate } from "@tanstack/react-router";
import {
  ArchiveConfirmPopUp,
  DeleteProjectPopUp,
  EditProjectPopUp,
  NewTaskPopup,
} from "@/components/index.comp";
import type { ProjectDetail } from "@/lib/api/types";
import type { DetailProjectPopup } from "./types";

interface PopupContainerProps {
  proyecto: ProjectDetail;
  popup: DetailProjectPopup | null;
  close: () => void;
}

// Todos los popups del detalle de proyecto
export function PopupContainer({ proyecto, popup, close }: PopupContainerProps) {
  const nav = useNavigate();
  const backToProjects = () => nav({ to: "/projects", replace: true });
  const onOpenChange = (open: boolean) => {
    if (!open) close();
  };

  return (
    <>
      <EditProjectPopUp isOpen={popup === "edit"} onOpenChange={onOpenChange} proyecto={proyecto} />
      <ArchiveConfirmPopUp
        isOpen={popup === "archive"}
        onOpenChange={onOpenChange}
        proyecto={proyecto}
        onArchived={backToProjects}
      />
      <DeleteProjectPopUp
        isOpen={popup === "delete"}
        onOpenChange={onOpenChange}
        proyecto={proyecto}
        onDeleted={backToProjects}
      />
      <NewTaskPopup
        isOpen={popup === "create-task"}
        onOpenChange={onOpenChange}
        proyecto={proyecto}
      />
    </>
  );
}
