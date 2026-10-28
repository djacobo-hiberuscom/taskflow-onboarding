import TAREAS from "@/assets/tempdata/tasks.json";
import PROYECTOS from "@/assets/tempdata/projects.json";
import { getUsernameById } from "@/tools/getUsername";
import { useParams } from "@tanstack/react-router";

export function DetailProject() {
  const { id } = useParams({ from: "/project/$id" });
  
  const projectTask = TAREAS.filter((t) => t.project_id === id);

  const proyecto = PROYECTOS.find((p) => p.id === id);
  if (!proyecto) {
    return <p>Proyecto no encontrado</p>;
  }
  

  return (
    <div className="flex items-center justify-center gap-3 p-5 border rounded-2xl m-10">
      <div>
        <h1>{proyecto.name}</h1>
        <p> {proyecto.description}</p>
        <h3>{getUsernameById(proyecto.created_by)}</h3>
        <h3>{proyecto.archived_at}</h3>
        <h3>{proyecto.created_at}</h3>
        <h3>{proyecto.updated_at}</h3>
      </div>

      <div className="flex flex-col justify-center items-center">
        {projectTask.length === 0 ? (
          <h1>Todavia no hay tareas</h1>
        ) : (
          projectTask.map((t) => {
            return <h1>{t.title}</h1>;
          })
        )}
      </div>
    </div>
  );
}
