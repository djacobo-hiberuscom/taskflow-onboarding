import { apiFetch } from "#lib/api/client";
import {
  type Project,
  type Page,
  type ProjectDetail,
  type ProjectInput,
} from "#lib/api/types";
import { projectKeys } from "#lib/query-keys";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

const BASE = "/api/v1/projects";

// Get all
export function useProjects({
  archived = false,
  page = 1,
  pageSize = 20,
} = {}) {
  return useQuery({
    queryKey: projectKeys.list({ archived, page, pageSize }),
    queryFn: () => {
      return apiFetch<Page<Project>>(
        `${BASE}?archived=${archived}&page=${page}&pageSize=${pageSize}`,
      );
    },
  });
}
// Get id
export function useProject(id: string | undefined) {
  return useQuery({
    queryKey: projectKeys.detail(id ?? ""),
    queryFn: () => {
      return apiFetch<ProjectDetail>(`${BASE}/${id}`);
    },
    enabled: !!id,
  });
}

// Crear proyecto
export function useCreateProject() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: ProjectInput) => {
      return apiFetch<Project>(BASE, {
        method: "POST",
        body: JSON.stringify(body),
      });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: projectKeys.all }),
  });
}
// editar
export function useUpdateProject() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...body }: ProjectInput & { id: string }) => {
      return apiFetch<Project>(`${BASE}/${id}`, {
        method: "PUT",
        body: JSON.stringify(body),
      });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: projectKeys.all }),
  });
}

// ARCHIVAR RESTAURAR ELIMINAR (SOLO ADMIN)
function useProjectAction(action: (id: string) => Promise<void>) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: action,
    onSuccess: () => qc.invalidateQueries({ queryKey: projectKeys.all }),
  });
}



// RESTORE
export function useRestoreProject() {
  return useProjectAction((id) =>
    apiFetch(`${BASE}/${id}/restore`, { method: "POST" }),
  );
}

// ARCHIVAR
export function useArchiveProject() {
  return useProjectAction((id) =>
    apiFetch(`${BASE}/${id}/archive`, { method: "POST" }),
  );
}

// DELETE
export function useDeleteProject() {
  const qc = useQueryClient() 
  return useMutation({
    mutationFn: (id: string) => apiFetch(`${BASE}/${id}`, { method: "DELETE" }),
    onSuccess: (__data, id) => {
        qc.removeQueries({queryKey: projectKeys.detail(id)})
        qc.invalidateQueries({queryKey:projectKeys.all})
    }
  })
}
