import { api, toQueryString } from "../client";
import type {
  Page,
  Project,
  ProjectDetail,
  ProjectInput,
  ProjectListParams,
} from "../types";

const BASE = "/api/v1/projects";

// Todas las llamadas HTTP de proyectos
export const projectsApi = {
  list: ({
    archived = false,
    page = 1,
    pageSize = 20,
  }: ProjectListParams = {}) =>
    api.get<Page<Project>>(
      `${BASE}?${toQueryString({ archived, page, pageSize })}`,
    ),
  get: (id: string) => api.get<ProjectDetail>(`${BASE}/${id}`),
  create: (body: ProjectInput) => api.post<Project>(BASE, body),
  update: (id: string, body: ProjectInput) =>
    api.put<Project>(`${BASE}/${id}`, body),
  archive: (id: string) => api.post(`${BASE}/${id}/archive`),
  restore: (id: string) => api.post(`${BASE}/${id}/restore`),
  remove: (id: string) => api.delete(`${BASE}/${id}`),
};
