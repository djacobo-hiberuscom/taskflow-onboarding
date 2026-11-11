import type { TaskPriority, TaskStatus } from "@/lib/api/types";

// para labels, opciones y colores de tareas

export const TASK_STATUS_LABEL: Record<TaskStatus, string> = {
  ToDo: "Pendiente",
  InProgress: "En curso",
  Done: "Completada",
};

export const TASK_PRIORITY_LABEL: Record<TaskPriority, string> = {
  High: "Alta",
  Medium: "Media",
  Low: "Baja",
};

export const TASK_STATUS_BADGE_CLASS: Record<TaskStatus, string> = {
  ToDo: "bg-status-todo text-status-todo-foreground border-current/15",
  InProgress: "bg-status-in-progress text-status-in-progress-foreground border-current/15",
  Done: "bg-status-done text-status-done-foreground border-current/15",
};

// Punto de color dentro del badge de estado
export const TASK_STATUS_DOT_CLASS: Record<TaskStatus, string> = {
  ToDo: "bg-status-todo-dot",
  InProgress: "bg-status-in-progress-dot",
  Done: "bg-status-done-dot",
};

export const TASK_PRIORITY_BADGE_CLASS: Record<TaskPriority, string> = {
  High: "bg-priority-high text-priority-high-foreground border-current/15",
  Medium: "bg-priority-medium text-priority-medium-foreground border-current/15",
  Low: "bg-priority-low text-priority-low-foreground border-current/15",
};

// Nº de barras rellenas en el icono de prioridad
export const TASK_PRIORITY_LEVEL: Record<TaskPriority, number> = {
  Low: 1,
  Medium: 2,
  High: 3,
};

// Opciones para los <FilterSelector />
export const TASK_STATUS_OPTIONS = (
  Object.keys(TASK_STATUS_LABEL) as TaskStatus[]
).map((value) => ({ value, label: TASK_STATUS_LABEL[value] }));

export const TASK_PRIORITY_OPTIONS = (
  Object.keys(TASK_PRIORITY_LABEL) as TaskPriority[]
).map((value) => ({ value, label: TASK_PRIORITY_LABEL[value] }));
