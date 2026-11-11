import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { projectsApi } from "@/lib/api/endpoints";
import type { ProjectInput, ProjectListParams } from "@/lib/api/types";
import { projectKeys } from "@/lib/query-keys";

// ---------- QUERIES ----------

// GET /projects
export function useProjects(
  params: ProjectListParams = {},
  options: { enabled?: boolean } = {},
) {
  const { archived = false, page = 1, pageSize = 20 } = params;
  const normalized = { archived, page, pageSize };
  return useQuery({
    queryKey: projectKeys.list(normalized),
    queryFn: () => projectsApi.list(normalized),
    ...options,
  });
}

// GET /projects/{id}
export function useProject(id: string | undefined) {
  return useQuery({
    queryKey: projectKeys.detail(id ?? ""),
    queryFn: () => projectsApi.get(id!),
    enabled: !!id,
  });
}

// ---------- MUTATIONS ----------

// Invalida todo lo de proyectos tras una mutación
function useInvalidateProjects() {
  const qc = useQueryClient();
  return () => qc.invalidateQueries({ queryKey: projectKeys.all });
}

// POST /projects
export function useCreateProject() {
  const invalidate = useInvalidateProjects();
  return useMutation({
    mutationFn: (body: ProjectInput) => projectsApi.create(body),
    onSuccess: invalidate,
  });
}

// PUT /projects/{id}
export function useUpdateProject() {
  const invalidate = useInvalidateProjects();
  return useMutation({
    mutationFn: ({ id, ...body }: ProjectInput & { id: string }) =>
      projectsApi.update(id, body),
    onSuccess: invalidate,
  });
}

// POST /projects/{id}/archive (solo admin)
export function useArchiveProject() {
  const invalidate = useInvalidateProjects();
  return useMutation({ mutationFn: projectsApi.archive, onSuccess: invalidate });
}

// POST /projects/{id}/restore (solo admin)
export function useRestoreProject() {
  const invalidate = useInvalidateProjects();
  return useMutation({ mutationFn: projectsApi.restore, onSuccess: invalidate });
}

// DELETE /projects/{id} (solo admin)
export function useDeleteProject() {
  const qc = useQueryClient();
  const invalidate = useInvalidateProjects();
  return useMutation({
    mutationFn: projectsApi.remove,
    onSuccess: (_data, id) => {
      qc.removeQueries({ queryKey: projectKeys.detail(id) });
      invalidate();
    },
  });
}
