import type { Project, Task } from "#lib/api/types";

// Buscar proyectos
export function SearchByProject (list: Project[], name: string) : Project[]{
    return list.filter((p) => p.name.toLocaleLowerCase().startsWith(name.toLocaleLowerCase()))
}

// bucar tareas
export function SearchByTask (list: Task[], name: string): Task[]{
    return list.filter((p) => p.title.toLocaleLowerCase().startsWith(name.toLocaleLowerCase()))
}