import USUARIOS from "@/assets/tempdata/users.json"
import PROYECTOS from "@/assets/tempdata/projects.json"
const users = USUARIOS


// Obtener nombre de usuario e un id
export function getUsernameById (id:string) {
    return users.find((u) => u.id === id)?.display_name
}


// obtener projecto por un id de una tarea
export function getProjectByTaskId(id :string){
    return PROYECTOS.find((p) => p.id === id)
}