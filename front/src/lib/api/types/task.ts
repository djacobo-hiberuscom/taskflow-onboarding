import { type UserSummary } from "../types";
// TAREAS
export type TaskStatus = "ToDo" | "InProgress" | "Done"
export type TaskPriority = "Low" | "Medium" | "High"

export interface Task { // TASK
  id: string,
  projectId: string,
  projectName: string,
  title:string,
  description: string | null,
  status: TaskStatus,
  priority: TaskPriority,
  dueDate: string | null,
  assignee: UserSummary | null,
  createdAt: string,
  updatedAt: string
}

// crear task
export interface CreateTaskInput { 
  title: string,
  description? : string,
  status?: TaskStatus
  priority: TaskPriority,
  dueDate?: string | null,
  assigneeId? : string | null
}
// Update task
export interface UpdateTaskInput {
  title: string,
  description?: null,
  status? : TaskStatus,
  priority: TaskPriority,
  dueDate?: string | null,
  assigneeId?: string
}