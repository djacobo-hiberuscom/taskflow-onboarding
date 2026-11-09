import Proyectos from "@/assets/tempdata/projects.json";
import Tareas from "@/assets/tempdata/tasks.json";
const PROYECTOS = Proyectos;
const TAREAS = Tareas;

// COMPONENTS
import { ChartPieDonutText} from "@/components/index.comp"
import { StatCard } from "./components/index";




// FEAT
export function DashboardFeat() {
  // (Start proyectos activos)
  const proyectosActivos = PROYECTOS.filter(
    (p) => p.archived_at !== null,
  ).length;

  // (Start ToDo)
  const tareasToDo = TAREAS.filter((t) => t.status === "ToDo").length;

  // (Start tareas completadas)
  const tareasCompletadas = TAREAS.filter((t) => t.status === "Done").length

  // (Start tareas inprogres)
  const tareasInProgress = TAREAS.filter((t) => t.status === "In Progress").length

  // (Start tareas incompletas) 
  const tareasIncompletas = tareasInProgress + tareasToDo


  // ------------
  return (
    <div className="p-5 h-full">
      <div id="title" className="flex flex-col gap-5"></div>
      <div id="body" className="flex flex-col gap-4">
        <div id="stats-contenedors" className="flex gap-5 justify-between">
          <StatCard name="Proyectos Activos" number={proyectosActivos}/>
          <StatCard name="Tareas abiertas" number={tareasIncompletas}/>
          <StatCard name="Tareas completadas" number={tareasCompletadas}/>
          <StatCard name="Proyectos Activos" number={12}/>
        </div>
        <div id="barra-tareas-estado">
          <ChartPieDonutText todo={tareasToDo} inProgress={tareasInProgress} done={tareasCompletadas}/>
        </div>
      </div>
    </div>
  );
}