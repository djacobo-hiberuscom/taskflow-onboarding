import {
  type CreateTaskInput,
  type Task,
  type TaskStatus,
} from "./../lib/api/types";
import { apiFetch } from "#lib/api/client";
import { projectKeys, taskKeys } from "#lib/query-keys";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

const BASE = "/api/v1/tasks";

function useInvalidateTask() {
  const qc = useQueryClient();
  return () => {
    qc.invalidateQueries({ queryKey: taskKeys.all });
    qc.invalidateQueries({ queryKey: projectKeys.all });
  };
}

// GET ALL

export function useTasks() {}

// GET ID /tasks/{id}
export function useTask(id: string | undefined) {
  return useQuery({
    queryKey: taskKeys.detail(id ?? ""),
    queryFn: () => apiFetch<Task>(`${BASE}/${id}`),
    enabled: !!id,
  });
}
// Crear task /projects/{projectId}/tasks
export function useCreateTask() {
  const invalidate = useInvalidateTask();
  return useMutation({
    mutationFn: ({
      projectId,
      ...body
    }: CreateTaskInput & { projectId: string }) =>
      apiFetch<Task>(`/api/v1/projects/${projectId}/tasks`, {
        method: "POST",
        body: JSON.stringify(body),
      }),
    onSuccess: invalidate,
  });
}

// actualizar estado task /tasks/{id}/status
export function useChangeTaskStatus() {
  const invalidate = useInvalidateTask();
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: TaskStatus }) =>
      apiFetch<Task>(`${BASE}/${id}/status`, {
        method: "PUT",
        body: JSON.stringify({ status }),
      }),
    onSuccess: invalidate,
  });
}

// asignar /task/{id}/assignee null = designar
export function useChangeTaskAssignee() {
  const invalidate = useInvalidateTask();
  return useMutation({
    mutationFn: ({
      id,
      assigneeId,
    }: {
      id: string;
      assigneeId: string | null;
    }) =>
      apiFetch<Task>(`${BASE}/${id}/assignee`, {
        method: "PUT",
        body: JSON.stringify({ assigneeId }),
      }),
    onSuccess: invalidate,
  });
}

// delete task /task/{id}
export function useDeleteTask() {
  const qc = useQueryClient();
  const invalidate = useInvalidateTask();
  return useMutation({
    mutationFn: (id: string) =>
      apiFetch(`${BASE}/${id}`, {
        method: "DELETE",
      }),
    onSuccess: (_data, id) => {
      qc.removeQueries({ queryKey: taskKeys.detail(id) });
      invalidate();
    },
  });
}
