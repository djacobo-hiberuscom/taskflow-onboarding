import { TaskPriorityBadge, TaskStatusBadge } from "@/components";
import type { Task } from "@/lib/api/types";
import { formatDate } from "@/lib/format";
import { TASK_GRID } from "./types";

interface RowTableTaskProps {
  tarea: Task;
  onClick: () => void;
}

// Fila de la tabla
export function RowTableTask({ tarea, onClick }: RowTableTaskProps) {
  return (
    <div
      className={`${TASK_GRID} w-full bg-background p-4 border-b hover:brightness-95 cursor-pointer`}
      onClick={onClick}
    >
      <span className="truncate">{tarea.title}</span>
      <span className="truncate">{tarea.projectName}</span>
      <span>
        <TaskStatusBadge status={tarea.status} />
      </span>
      <span>
        <TaskPriorityBadge priority={tarea.priority} />
      </span>
      <span className="truncate">{tarea.assignee?.displayName ?? "Sin asignar"}</span>
      <span>{formatDate(tarea.dueDate)}</span>
    </div>
  );
}
