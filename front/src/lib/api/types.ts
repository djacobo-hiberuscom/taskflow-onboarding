// User sumary
export interface UserSummary {
  id: string;
  displayName: string;
}
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

// TAREAS
export interface Task {
  id: string;
  project_id: string;
  assigned_id: string;
  title: string;
  description: string;
  status: string;
  priority: string;
  due_date: string;
  created_at: string;
  update_at: string;
}

// USER
export interface User {
  id: string;
  keycloak_sub: string;
  display_name: string;
  email: string;
  created_at: Date;
}
