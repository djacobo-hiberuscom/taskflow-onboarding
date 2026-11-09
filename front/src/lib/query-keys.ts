// projects
export const projectKeys = {
    all: ["project"] as const,
    list: (p: {archived: boolean; page: number; pageSize: number}) => ["project", p] as const,
    detail: (id:string) => ["project", id] as const
}

// Task
export const taskKeys = {
    all: ["task"] as const,
    detail: (id:string) => ["task", id] as const
}