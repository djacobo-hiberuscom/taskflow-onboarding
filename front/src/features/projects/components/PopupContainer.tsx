import {
  ArchiveConfirmPopUp,
  ArchivedPopUp,
  DeleteProjectPopUp,
  EditProjectPopUp,
  NewProjectPopup,
} from "@/components";
import type { Project } from "@/lib/api/types";
import type { ProjectsPopup } from "./types";

interface PopupContainerProps {
  popup: ProjectsPopup | null;
  proyecto?: Project;
  close: () => void;
}

// Todos los popups de la feat de proyectos
export function PopupContainer({ popup, proyecto, close }: PopupContainerProps) {
  const onOpenChange = (open: boolean) => {
    if (!open) close();
  };

  return (
    <>
      <NewProjectPopup isOpen={popup === "new"} onOpenChange={onOpenChange} />
      <ArchivedPopUp isOpen={popup === "archived-list"} onOpenChange={onOpenChange} />
      <EditProjectPopUp isOpen={popup === "edit"} onOpenChange={onOpenChange} proyecto={proyecto} />
      <ArchiveConfirmPopUp
        isOpen={popup === "archive"}
        onOpenChange={onOpenChange}
        proyecto={proyecto}
      />
      <DeleteProjectPopUp
        isOpen={popup === "delete"}
        onOpenChange={onOpenChange}
        proyecto={proyecto}
      />
    </>
  );
}
