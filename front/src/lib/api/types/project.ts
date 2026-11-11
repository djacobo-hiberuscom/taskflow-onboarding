import type { PageParams } from "./common";
import type { UserSummary } from "./user";

// GET /api/v1/projects (el listado NO incluye miembros)
export interface Project {
  id: string;
  name: string;
  description: string | null;
  createdBy: UserSummary | null;
  archivedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

// GET /api/v1/projects/{id}
export interface ProjectDetail extends Project {
  members: UserSummary[];
}

// POST / PUT
export interface ProjectInput {
  name: string;
  description?: string | null;
}

// Filtros del listado
export interface ProjectListParams extends PageParams {
  archived?: boolean;
}
