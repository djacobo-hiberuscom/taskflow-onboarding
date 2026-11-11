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
  ToDo: "bg-[#FEF3C7] text-[#92400E]",
  InProgress: "bg-[#DBEAFE] text-[#1E40AF]",
  Done: "bg-[#DCFCE7] text-[#166534]",
};

export const TASK_PRIORITY_BADGE_CLASS: Record<TaskPriority, string> = {
  High: "bg-[#FEE2E2] text-[#991B1B]",
  Medium: "bg-[#FEF3C7] text-[#92400E]",
  Low: "bg-[#F1F5F9] text-[#334155]",
};

// Opciones para los <FilterSelector />
export const TASK_STATUS_OPTIONS = (
  Object.keys(TASK_STATUS_LABEL) as TaskStatus[]
).map((value) => ({ value, label: TASK_STATUS_LABEL[value] }));

export const TASK_PRIORITY_OPTIONS = (
  Object.keys(TASK_PRIORITY_LABEL) as TaskPriority[]
).map((value) => ({ value, label: TASK_PRIORITY_LABEL[value] }));
