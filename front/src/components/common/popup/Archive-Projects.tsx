import { useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import {
  Archive,
  ArchiveRestore,
  ExternalLink,
  ListChecks,
  Trash,
  Users,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Pagination } from "@/components/common/Pagination";
import { QueryState } from "@/components/common/QueryState";
import {
  useProject,
  useProjects,
  useRestoreProject,
} from "@/hooks/use-project";
import { useTaskCount } from "@/hooks/use-task";
import type { Project } from "@/lib/api/types";
import { formatDate } from "@/lib/format";
import { notify } from "@/lib/notify";
import { DeleteProjectPopUp } from "./Delete-Project";

const PAGE_SIZE = 7;

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
        onOpenChange={(open) => {
          if (!open) setToDelete(null);
        }}
        proyecto={toDelete ?? undefined}
        onDeleted={() => {
          if (data?.items.length === 1 && page > 1) setPage(page - 1);
        }}
      />
      <Dialog open={isOpen} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-6xl h-130 max-h-[90vh] flex flex-col p-4 gap-3">
          <DialogHeader>
            <DialogTitle className="text-2xl flex items-center gap-2">
              <Archive className="size-5" /> Archivados
              {data && (
                <span className="text-muted-foreground font-normal">
                  · {data.totalCount}
                </span>
              )}
            </DialogTitle>
            <DialogDescription>
              Los proyectos archivados no aparecen en el panel ni en los filtros
              de tareas.
            </DialogDescription>
          </DialogHeader>

          <div className="flex-1 min-h-0 overflow-y-auto border-y">
            {isLoading || error ? (
              <QueryState isLoading={isLoading} error={error} />
            ) : data?.items.length ? (
              <Table>
                <TableHeader className="bg-muted/50">
                  <TableRow>
                    <TableHead className="text-muted-foreground">
                      Proyecto
                    </TableHead>
                    <TableHead className="text-muted-foreground">
                      Miembros
                    </TableHead>
                    <TableHead className="text-muted-foreground">
                      Tareas
                    </TableHead>
                    <TableHead className="text-muted-foreground">
                      Archivado
                    </TableHead>
                    <TableHead className="text-muted-foreground text-right">
                      Acciones
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((p) => (
                    <ArchivedRow
                      key={p.id}
                      proyecto={p}
                      onOpen={() => handleOpen(p)}
                      onRestore={() => handleRestore(p)}
                      onDelete={() => setToDelete(p)}
                    />
                  ))}
                </TableBody>
              </Table>
            ) : (
              <p className="h-full flex items-center justify-center text-muted-foreground">
                No hay proyectos archivados
              </p>
            )}
          </div>

          <DialogFooter>
            <Pagination
              page={page}
              totalPages={totalPages}
              onPageChange={setPage}
            />
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
function ArchivedRow({
  proyecto,
  onOpen,
  onRestore,
  onDelete,
}: ArchivedRowProps) {
  const { data: detail } = useProject(proyecto.id);
  const { data: nTareas = 0 } = useTaskCount({ projectId: proyecto.id });
  const nMiembros = detail?.members.length ?? 0;

  return (
    <TableRow>
      <TableCell className="py-3">
        <p className="font-medium text-sm">{proyecto.name}</p>
        {proyecto.description && (
          <p className="text-xs text-muted-foreground truncate max-w-70">
            {proyecto.description}
          </p>
        )}
      </TableCell>

      <TableCell className="py-2">
        <span className="flex items-center gap-1.5">
          <Users className="size-3.5 text-muted-foreground" /> {nMiembros}
        </span>
      </TableCell>

      <TableCell className="py-2">
        <span className="flex items-center gap-1.5">
          <ListChecks className="size-3.5 text-muted-foreground" /> {nTareas}
        </span>
      </TableCell>

      <TableCell className="py-2 text-muted-foreground">
        {formatDate(proyecto.archivedAt)}
      </TableCell>

      <TableCell className="py-2">
        <div className="flex justify-end gap-1">
          <Button size="sm" variant="outline" onClick={onOpen}>
            <ExternalLink /> Abrir
          </Button>
          <Button size="sm" variant="outline" onClick={onRestore}>
            <ArchiveRestore /> Restaurar
          </Button>
          <Button
            size="sm"
            variant="outline"
            className="text-destructive hover:text-destructive"
            onClick={onDelete}
          >
            <Trash /> Eliminar
          </Button>
        </div>
      </TableCell>
    </TableRow>
  );
}
