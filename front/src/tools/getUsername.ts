import USUARIOS from "@/assets/tempdata/users.json"
import PROYECTOS from "@/assets/tempdata/projects.json"
import TAREAS from "@/assets/tempdata/tasks.json"
const users = USUARIOS


// Obtener nombre de usuario e un id
export function getUsernameById (id?:string) {
    return users.find((u) => u.id === id)?.display_name
}


// obtener projecto por un id de una tarea
export function getProjectByTaskId(id :string){
    return PROYECTOS.find((p) => p.id === id)
}

// lista nombres usuario dun proyecto
export function getUsernameByProjectId(id: string){
    const tareasProyecto = TAREAS.filter((t) => t.project_id === id) // LISTA DE TAREAS
    return [... new Set(tareasProyecto.map((t) => t.assigned_id))]
}