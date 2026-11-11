import type { TaskPriority, TaskStatus } from "@/lib/api/types";

// Filtros de la vista de tareas
export interface TaskFilters {
  project: string | null;
  status: TaskStatus | null;
  priority: TaskPriority | null;
  member: string | null;
}

export const EMPTY_FILTERS: TaskFilters = {
  project: null,
  status: null,
  priority: null,
  member: null,
};

// Grid compartido por la cabecera y las filas de la tabla
export const TASK_GRID = "grid grid-cols-6 items-center gap-3";
