import { useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { Archive, ArrowRight, RefreshCw, Trash } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Pagination } from "@/components/common/Pagination";
import { QueryState } from "@/components/common/QueryState";
import { useProject, useProjects, useRestoreProject } from "@/hooks/use-project";
import { useTaskCount } from "@/hooks/use-task";
import type { Project } from "@/lib/api/types";
import { formatDate } from "@/lib/format";
import { notify } from "@/lib/notify";
import { DeleteProjectPopUp } from "./Delete-Project";

const PAGE_SIZE = 5;

interface ArchivedPopUpProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
}

// Listado de proyectos archivados (solo admin)
export function ArchivedPopUp({ isOpen, onOpenChange }: ArchivedPopUpProps) {
  const nav = useNavigate();
  const [page, setPage] = useState(1);
  const [toDelete, setToDelete] = useState<Project | null>(null);

  const { data, isLoading, error } = useProjects(
    { archived: true, page, pageSize: PAGE_SIZE },
    { enabled: isOpen },
  );
  const restoreProject = useRestoreProject();

  const totalPages = Math.ceil((data?.totalCount ?? 0) / PAGE_SIZE);

  const handleRestore = (proyecto: Project) => {
    restoreProject.mutate(proyecto.id, {
      onSuccess: () => notify.success("Proyecto restaurado correctamente"),
      onError: () => notify.error("Error al restaurar el proyecto"),
    });
  };

  const handleOpen = (proyecto: Project) => {
    onOpenChange(false);
    nav({ to: "/projects/$id", params: { id: proyecto.id } });
  };

  return (
    <>
      <DeleteProjectPopUp
        isOpen={toDelete !== null}
        proyecto={toDelete ?? undefined}
        onOpenChange={(open) => {
          if (!open) setToDelete(null);
        }}
      />
      <Dialog open={isOpen} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-6xl max-h-[90vh] flex flex-col p-4 gap-3">
          <DialogHeader>
            <DialogTitle className="text-2xl">Archivados</DialogTitle>
            <DialogDescription>Proyectos archivados</DialogDescription>
          </DialogHeader>

          <div className="flex-1 overflow-y-auto">
            {isLoading || error ? (
              <QueryState isLoading={isLoading} error={error} />
            ) : data?.items.length ? (
              data.items.map((p) => (
                <ArchivedRow
                  key={p.id}
                  proyecto={p}
                  onOpen={() => handleOpen(p)}
                  onRestore={() => handleRestore(p)}
                  onDelete={() => setToDelete(p)}
                />
              ))
            ) : (
              <p className="p-6 text-center text-muted-foreground">No hay proyectos archivados</p>
            )}
          </div>

          <DialogFooter>
            <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

// Fila de un proyecto archivado
interface ArchivedRowProps {
  proyecto: Project;
  onOpen: () => void;
  onRestore: () => void;
  onDelete: () => void;
}
function ArchivedRow({ proyecto, onOpen, onRestore, onDelete }: ArchivedRowProps) {
  const { data: detail } = useProject(proyecto.id);
  const { data: nTareas = 0 } = useTaskCount({ projectId: proyecto.id });
  const nMiembros = detail?.members.length ?? 0;

  return (
    <div className="border-y flex justify-between items-center gap-4 p-6">
      <div className="flex-1">
        <h1 className="text-xl">{proyecto.name}</h1>
        <div className="flex gap-4">
          <span>{nMiembros} miembros</span>
          <span>{nTareas} tareas</span>
        </div>
      </div>

      <Badge className="p-4 text-2xs bg-[#DCFCE7] text-[#166534]">
        <Archive /> Archivado {formatDate(proyecto.archivedAt)}
      </Badge>

      <div className="flex gap-1">
        <Button variant="outline" onClick={onOpen}>
          <ArrowRight /> Abrir
        </Button>
        <Button variant="outline" onClick={onRestore}>
          <RefreshCw /> Restaurar
        </Button>
        <Button variant="outline" className="text-red-500" onClick={onDelete}>
          <Trash /> Eliminar
        </Button>
      </div>
    </div>
  );
}
