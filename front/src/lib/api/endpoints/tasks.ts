import { api, toQueryString } from "../client";
import type {
  CreateTaskInput,
  Page,
  Task,
  TaskListParams,
  TaskStatus,
} from "../types";

const BASE = "/api/v1/tasks";

// Todas las llamadas HTTP de tareas
export const tasksApi = {
  list: ({ page = 1, pageSize = 20, ...filters }: TaskListParams = {}) =>
    api.get<Page<Task>>(
      `${BASE}?${toQueryString({ ...filters, page, pageSize })}`,
    ),
  get: (id: string) => api.get<Task>(`${BASE}/${id}`),
  create: (projectId: string, body: CreateTaskInput) =>
    api.post<Task>(`/api/v1/projects/${projectId}/tasks`, body),
  changeStatus: (id: string, status: TaskStatus) =>
    api.put<Task>(`${BASE}/${id}/status`, { status }),
  // assigneeId = null -> desasignar
  changeAssignee: (id: string, assigneeId: string | null) =>
    api.put<Task>(`${BASE}/${id}/assignee`, { assigneeId }),
  remove: (id: string) => api.delete(`${BASE}/${id}`),
};
