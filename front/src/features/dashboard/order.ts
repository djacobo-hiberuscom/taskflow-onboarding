import type { Project, Task } from "#lib/api/types";

// PROJECTS
// Nombre
export function ProjectOrderByName (list: Project[]){
    return list.sort((a, b) => a.name.localeCompare(b.name))
}

// Deescripcion
export function ProjectOrderByDescription (list: Project[]){
    return list.sort((a, b) => a.description.localeCompare(b.description))
}
// Created At
export function ProjectOrderByCreated (list: Project[]) {
    return list.sort((a, b) => a.created_at.localeCompare(b.created_at))
}
// last Change
export function ProjectOrderByLastChange (list: Project[]) {
    return list.sort((a, b) => a.updated_at.localeCompare(b.updated_at))
}

// TASK
// Nombre
export function TaskOrderByName (list: Task[]){
    return list.sort((a, b) => a.title.localeCompare(b.title))
}

// Deescripcion
export function TaskOrderByDescription (list: Task[]){
    return list.sort((a, b) => a.description.localeCompare(b.description))
}
// Created At
export function TaskOrderByCreated (list: Task[]) {
    return list.sort((a, b) => a.created_at.localeCompare(b.created_at))
}
// last Change
export function TaskOrderByLastChange (list: Task[]) {
    return list.sort((a, b) => a.update_at.localeCompare(b.update_at))
}

// status
export function TaskOrderByStatus (list: Task[]) {
    return list.sort((a, b) => a.status.localeCompare(b.status))
}

// priority
export function TaskOrderByPriority (list: Task[]) {
    return list.sort((a, b) => a.priority.localeCompare(b.priority))
}
