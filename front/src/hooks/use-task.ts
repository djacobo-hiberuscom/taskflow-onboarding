import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { tasksApi } from "@/lib/api/endpoints";
import type { CreateTaskInput, TaskListParams, TaskStatus } from "@/lib/api/types";
import { projectKeys, taskKeys } from "@/lib/query-keys";

// ---------- QUERIES ----------

// GET /tasks
export function useTasks(params: TaskListParams = {}) {
  return useQuery({
    queryKey: taskKeys.list(params),
    queryFn: () => tasksApi.list(params),
    placeholderData: keepPreviousData,
  });
}



// Solo el total de tareas que cumplen unos filtros (pide 1 item)
export function useTaskCount(
  params: Omit<TaskListParams, "page" | "pageSize"> = {},
  options: { enabled?: boolean } = {},
) {
  const listParams = { ...params, page: 1, pageSize: 1 };
  return useQuery({
    queryKey: taskKeys.list(listParams),
    queryFn: () => tasksApi.list(listParams),
    select: (page) => page.totalCount,
    ...options,
  });
}
// use all task 
export function useAllTask (params: Omit<TaskListParams, "page" | "pageSize"> = {}){
  return useQuery({
    queryKey: [...taskKeys.list(params), "all"],
    queryFn: async () => {
      const pageSize = 100;
      const first = await tasksApi.list({ ...params, page: 1, pageSize });
      const totalPages = Math.ceil(first.totalCount / pageSize);
      const rest = await Promise.all(
        Array.from({ length: Math.max(totalPages - 1, 0) }, (_, i) =>
          tasksApi.list({ ...params, page: i + 2, pageSize }),
        ),
      );
      return [first, ...rest].flatMap((p) => p.items);
    },
  });
}

// GET /tasks/{id}
export function useTask(id: string | undefined) {
  return useQuery({
    queryKey: taskKeys.detail(id ?? ""),
    queryFn: () => tasksApi.get(id!),
    enabled: !!id,
  });
}

// ---------- MUTATIONS ----------

// Una tarea afecta a listados de tareas y a contadores de proyectos
function useInvalidateTasks() {
  const qc = useQueryClient();
  return () => {
    qc.invalidateQueries({ queryKey: taskKeys.all });
    qc.invalidateQueries({ queryKey: projectKeys.all });
  };
}

// POST /projects/{projectId}/tasks
export function useCreateTask() {
  const invalidate = useInvalidateTasks();
  return useMutation({
    mutationFn: ({ projectId, ...body }: CreateTaskInput & { projectId: string }) =>
      tasksApi.create(projectId, body),
    onSuccess: invalidate,
  });
}

// PUT /tasks/{id}/status
export function useChangeTaskStatus() {
  const invalidate = useInvalidateTasks();
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: TaskStatus }) =>
      tasksApi.changeStatus(id, status),
    onSuccess: invalidate,
  });
}

// PUT /tasks/{id}/assignee (assigneeId null = desasignar)
export function useChangeTaskAssignee() {
  const invalidate = useInvalidateTasks();
  return useMutation({
    mutationFn: ({ id, assigneeId }: { id: string; assigneeId: string | null }) =>
      tasksApi.changeAssignee(id, assigneeId),
    onSuccess: invalidate,
  });
}

// DELETE /tasks/{id}
export function useDeleteTask() {
  const qc = useQueryClient();
  const invalidate = useInvalidateTasks();
  return useMutation({
    mutationFn: tasksApi.remove,
    onSuccess: (_data, id) => {
      qc.removeQueries({ queryKey: taskKeys.detail(id) });
      invalidate();
    },
  });
}
