import type { ProjectListParams, TaskListParams } from "./api/types";

// Claves de caché de TanStack Query.
// Invalidar `xxxKeys.all` refresca listados y detalles a la vez.

export const projectKeys = {
  all: ["projects"] as const,
  list: (params: ProjectListParams) =>
    [...projectKeys.all, "list", params] as const,
  detail: (id: string) => [...projectKeys.all, "detail", id] as const,
};

export const taskKeys = {
  all: ["tasks"] as const,
  list: (params: TaskListParams) => [...taskKeys.all, "list", params] as const,
  detail: (id: string) => [...taskKeys.all, "detail", id] as const,
};

export const userKeys = {
  all: ["users"] as const,
  me: () => [...userKeys.all, "me"] as const,
  assignable: () => [...userKeys.all, "assignable"] as const,
};
