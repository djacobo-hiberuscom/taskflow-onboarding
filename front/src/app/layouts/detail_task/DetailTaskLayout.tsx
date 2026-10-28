import TAREAS from "@/assets/tempdata/tasks.json"
import { useParams } from "@tanstack/react-router"


export function DetailTask () {
    const {id} = useParams({from: "/task/$id"})

    const tarea = TAREAS.find((t) => t.id === id)
    if  (!tarea) {
        return (
            <div className="felx items-center justify-center h-dvh">
                <h1>Tarea no encontrada</h1>
            </div>
        )
    }
    return (
        <div className="flex items-center justify-center">
            <h1>{tarea.title}</h1>
        </div>
    )
}