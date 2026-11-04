// COMPONENTES
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Button,
  Badge,
} from "@/components/index.comp";
// ICONOS
import { Archive, Trash, RefreshCw, ArrowRight } from "lucide-react";

// PROYECTOS
import type { Project } from "#lib/api/types";
import { ApiError } from "#lib/api/client";
import { NotFound } from "../../index.featues";

// TAREAS
import TAREAS from "@/assets/tempdata/tasks.json";

// HOOKS

import { getUsernameByProjectId } from "@/tools/getUsername";
import { useState } from "react";
import { useProjects, useRestoreProject } from "#hooks/use-project";
import { DeleteConfirmPopUp } from "./delete-confirm.popup";

interface parameters {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
}

export function ArchivedPopUp({ isOpen, onOpenChange }: parameters) {
  const { data, isLoading, error } = useProjects({ archived: true });
  const useRestore = useRestoreProject();
  const [page, setPage] = useState(1);
  const [toDelete, setToDelete] = useState<Project | null>(null);
  const [toRestore, setToRestore] = useState<Project | null>(null);
  const PAGE_SIZE = 5;

  const visibles = data?.items.filter((p) => p.archivedAt !== null) ?? [];

  if (isLoading) return <h1>Cargando...</h1>;
  if (error instanceof ApiError && error.status === 404) return <NotFound />;
  if (error) return <h1>{error.message}</h1>;

  const totalPage = Math.ceil(visibles.length / PAGE_SIZE);

  
  
  const handleClicRestore = () => {
    if (!toRestore) return;
    useRestore.mutate(toRestore.id, {
      onSuccess: () => {},
    });
  };

  return (
    <div>
      <DeleteConfirmPopUp
        onOpenChange={(open) => {
          if (!open) setToDelete(null);
        }}
        isOpen={toDelete !== null}
        proyecto={toDelete ?? undefined}
      />
      <Dialog open={isOpen} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-6xl max-h-[90vh] flex flex-col p-4 gap-3">
          <DialogHeader>
            <DialogTitle className="text-2xl"> Archivados</DialogTitle>
            <DialogDescription>Proyectos archivados</DialogDescription>
          </DialogHeader>
          <div className="flex-1 overflow-y-auto">
            {visibles
              .slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE)
              .map((pa) => {
                return (
                  <ArchivedRow
                    proyecto={pa}
                    onDeleteProject={() => setToDelete(pa)}
                    onOpenProject={() => {}}
                    onRestoreProject={() => {
                      setToRestore(pa);
                      handleClicRestore();
                    }}
                  />
                );
              })}
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                if (page - 1 > 0) setPage(page - 1);
              }}
            >
              Anterior
            </Button>
            <h1 className="text-2xl">
              {page}/{totalPage}
            </h1>
            <Button
              variant="outline"
              onClick={() => {
                if (page + 1 <= totalPage) setPage(page + 1);
              }}
            >
              Siguiente
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

// COMPONENTES DEL POPUP
interface recyclerParam {
  proyecto: Project;
  onOpenProject: () => void;
  onRestoreProject: () => void;
  onDeleteProject: () => void;
}
function ArchivedRow({
  proyecto,
  onOpenProject,
  onRestoreProject,
  onDeleteProject,
}: recyclerParam) {
  const nTareas = TAREAS.filter((t) => t.project_id === proyecto.id).length;
  return (
    <div className="border-t border-b flex justify-between items-center gap-4 p-6">
      <div className="flex-1">
        <h1 className="text-xl">{proyecto.name}</h1>
        <div className="flex gap-4">
          <span>{getUsernameByProjectId(proyecto.id).length} miembros</span>
          <span>{nTareas} tareas</span>
        </div>
      </div>

      <Badge className="p-4 text-2xs bg-[#DCFCE7] text-[#166534]">
        <Archive /> Archivado {proyecto.archivedAt}
      </Badge>

      <div className="flex gap-1">
        <Button variant="outline" onClick={onOpenProject}>
          <ArrowRight /> Abrir
        </Button>
        <Button variant="outline" onClick={onRestoreProject}>
          <RefreshCw /> Restaurar
        </Button>
        <Button
          variant="outline"
          className="text-red-500"
          onClick={onDeleteProject}
        >
          <Trash /> Eliminar
        </Button>
      </div>
    </div>
  );
}
