import type { PageParams } from "./common";
import type { UserSummary } from "./user";

export type TaskStatus = "ToDo" | "InProgress" | "Done";
export type TaskPriority = "Low" | "Medium" | "High";

// GET /api/v1/tasks
export interface Task {
  id: string;
  projectId: string;
  projectName: string;
  title: string;
  description: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  dueDate: string | null;
  assignee: UserSummary | null;
  createdAt: string;
  updatedAt: string;
}

// POST /api/v1/projects/{projectId}/tasks
export interface CreateTaskInput {
  title: string;
  description?: string;
  status?: TaskStatus;
  priority: TaskPriority;
  dueDate?: string | null;
  assigneeId?: string | null;
}

// PUT /api/v1/tasks/{id}
export interface UpdateTaskInput {
  title: string;
  description?: string | null;
  status?: TaskStatus;
  priority: TaskPriority;
  dueDate?: string | null;
  assigneeId?: string | null;
}

// Filtros del listado ("unassigned" en assigneeId = tareas sin asignar)
export interface TaskListParams extends PageParams {
  projectId?: string;
  status?: TaskStatus[];
  priority?: TaskPriority[];
  assigneeId?: string;
  sort?: "createdAt" | "dueDate";
  order?: "asc" | "desc";
}
