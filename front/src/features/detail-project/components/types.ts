import type { TaskPriority, TaskStatus } from "@/lib/api/types";

// Popups que puede abrir el detalle de proyecto
export type DetailProjectPopup = "edit" | "archive" | "delete" | "create-task";

// Filtros de la tabla de tareas
export interface TaskFilters {
  status: TaskStatus | null;
  priority: TaskPriority | null;
  member: string | null;
}

export const EMPTY_FILTERS: TaskFilters = { status: null, priority: null, member: null };
