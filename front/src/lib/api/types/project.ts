import { type UserSummary } from "../types";

// PROYECTOS
export interface Project {
  id: string;
  name: string;
  description: string | null;
  createdBy: UserSummary | null;
  archivedAt: string | null;
  createdAt: string;
  updatedAt: string;
  members: UserSummary[]
}

// Projct details
export interface ProjectDetail extends Project {
    members: UserSummary[]
}

// PAGE
export interface Page<T> {
    items: T[],
    page: number,
    pageSize: number
    totalCount:number
}

// PRoject input
export interface ProjectInput {
    name:string,
    description?: string | null
}