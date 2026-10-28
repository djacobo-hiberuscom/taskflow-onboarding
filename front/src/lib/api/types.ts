// PROYECTOS
export interface Project{
    id: string
    name: string
    description: string
    created_by: string
    archived_at?: string | null
    created_at: string
    updated_at: string
}

// TAREAS
export interface Task {
    id:string
    project_id: string
    assigned_id: string
    title:string
    description: string
    status: string
    priority: string
    due_date: string
    created_at: string
    update_at: string
}

// USER
export interface User {
    id: string
    keycloak_sub: string
    display_name: string
    email: string
    created_at: Date
}