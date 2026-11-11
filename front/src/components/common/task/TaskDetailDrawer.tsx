import { useState, type ReactNode } from "react";
import { Folder } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Drawer, DrawerContent, DrawerFooter } from "@/components/ui/drawer";
import { Separator } from "@/components/ui/separator";
import { TaskPriorityBadge } from "@/components/common/badges/TaskBadges";
import { FilterSelector } from "@/components/common/FilterSelector";
import { ConfirmDeletePopUp } from "@/components/common/popup/Delete-Confirm";
import { QueryState } from "@/components/common/QueryState";
import { useAuth } from "@/hooks/use-auth";
import { useProject } from "@/hooks/use-project";
import {
  useChangeTaskAssignee,
  useChangeTaskStatus,
  useDeleteTask,
  useTask,
} from "@/hooks/use-task";
import type { ProjectDetail, Task } from "@/lib/api/types";
import { TASK_STATUS_OPTIONS } from "@/lib/constants/task";
import { formatDate, formatDateTime } from "@/lib/format";
import { notify } from "@/lib/notify";

interface TaskDetailDrawerProps {
  taskId: string | null; // null = cerrado
  onClose: () => void;
}

// Drawer lateral con el detalle de una tarea (usado en Tareas y en Detalle de proyecto)
export function TaskDetailDrawer({ taskId, onClose }: TaskDetailDrawerProps) {
  const [confirmDelete, setConfirmDelete] = useState(false);

  const { data: task, isLoading, error } = useTask(taskId ?? undefined);
  const { data: project } = useProject(task?.projectId);
  const deleteTask = useDeleteTask();

  const handleDelete = () => {
    if (!task) return;
    deleteTask.mutate(task.id, {
      onSuccess: () => {
        setConfirmDelete(false);
        onClose();
        notify.success("Se ha borrado correctamente la tarea");
      },
      onError: () => {
        setConfirmDelete(false);
        notify.error("Error al borrar la tarea");
      },
    });
  };

  return (
    <Drawer
      modal={false}
      disablePointerDismissal
      swipeDirection="right"
      open={taskId !== null}
      onOpenChange={(open) => {
        if (!open) onClose();
      }}
    >
      <ConfirmDeletePopUp
        modal={false}
        isOpen={confirmDelete}
        onOpenChange={setConfirmDelete}
        title={`¿Eliminar: ${task?.title ?? ""}?`}
        description="La tarea se borrará de forma permanente"
        onConfirm={handleDelete}
        isPending={deleteTask.isPending}
      />
      <DrawerContent>
        {task ? (
          <TaskDetail task={task} project={project} />
        ) : (
          <QueryState isLoading={isLoading} error={error} />
        )}
        <DrawerFooter>
          <div className="flex justify-end">
            <Button variant="destructive" disabled={!task} onClick={() => setConfirmDelete(true)}>
              Eliminar
            </Button>
          </div>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  );
}

// Contenido del drawer
function TaskDetail({ task, project }: { task: Task; project?: ProjectDetail }) {
  const { isAdmin } = useAuth();
  const changeStatus = useChangeTaskStatus();
  const changeAssignee = useChangeTaskAssignee();

  const memberOptions = (project?.members ?? []).map((m) => ({
    label: m.displayName,
    value: m.id,
  }));

  return (
    <div className="p-2">
      <div className="border-b p-2 flex gap-2 items-center">
        <Folder />
        <h2 className="text-xs">{task.projectName}</h2>
      </div>

      <div className="flex flex-col flex-1 p-2 gap-4">
        <h1 className="text-xl font-bold">{task.title}</h1>

        <div className="grid grid-cols-2 gap-2">
          <div className="min-w-0 flex flex-col gap-1">
            <span>Estado</span>
            <FilterSelector
              className="w-full"
              items={TASK_STATUS_OPTIONS}
              placeholder="Estado actual"
              value={task.status}
              disabled={changeStatus.isPending}
              onChange={(status) => {
                if (status && status !== task.status)
                  changeStatus.mutate({ id: task.id, status });
              }}
            />
          </div>
          <div className="min-w-0 flex flex-col gap-1">
            <span>Asignado</span>
            <FilterSelector
              className="w-full"
              items={memberOptions}
              placeholder="Miembro asignado"
              value={task.assignee?.id ?? null}
              disabled={!isAdmin || changeAssignee.isPending}
              onChange={(assigneeId) => {
                if (assigneeId && assigneeId !== task.assignee?.id)
                  changeAssignee.mutate({ id: task.id, assigneeId });
              }}
            />
          </div>
        </div>

        <Separator />

        <div>
          <h2 className="font-bold">Descripción</h2>
          <p>{task.description}</p>
        </div>

        <div>
          <DetailRow label="Prioridad">
            <TaskPriorityBadge priority={task.priority} />
          </DetailRow>
          <DetailRow label="Vencimiento">{formatDate(task.dueDate)}</DetailRow>
          <DetailRow label="Proyecto">{task.projectName}</DetailRow>
          <DetailRow label="Creada">{formatDateTime(task.createdAt)}</DetailRow>
          <DetailRow label="Último cambio">{formatDateTime(task.updatedAt)}</DetailRow>
        </div>
      </div>
    </div>
  );
}

function DetailRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="border-y p-5 flex">
      <h3 className="flex-1">{label}</h3>
      <span>{children}</span>
    </div>
  );
}
