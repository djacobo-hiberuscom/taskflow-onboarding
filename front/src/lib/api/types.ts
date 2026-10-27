// PROYECTOS
export interface Project{
    id: string
    name: string
    description: string
    created_by: string
    archived_at?: Date | null
    created_at: Date
    updated_at: Date
}

// TAREAS
export interface Task {
    id:string
    project_id: string
    assigned_id:string
    title:string
    description: string
    status: string
    priority: string
    due_date: Date
    created_at: Date
    update_at: Date
}

// USER
export interface User {
    id: string
    keycloak_sub: string
    display_name: string
    email: string
    created_at: Date
}