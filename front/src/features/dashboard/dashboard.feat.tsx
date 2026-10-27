import { DataTable, Input } from "@/components/index.comp";
import {
  ProjectColum,
  TaskColum,
} from "@/app/layouts/app/components/index.comp";
import Proyectos from "@/assets/tempdata/projects.json";
import Tareas from "@/assets/tempdata/tasks.json";
import { useState } from "react";
import { SearchByProject, SearchByTask } from "./search";

const PROYECTOS = Proyectos
const TAREAS = Tareas

export function DashboardFeat() {
  const [searchProject, setSeachProject] = useState("");
  const [searchTask, setSeachTask] = useState("");

  const proyectos = SearchByProject(PROYECTOS, searchProject)
  const tareas = SearchByTask(TAREAS, searchTask)

  return (
    <div className="p-5 h-full">
      <div className="flex flex-col gap-5">
        <div className="flex flex-col gap-3">
          <h2>Proyectos</h2>
          <div className="flex gap-3">
            <Input
              placeholder="Buscar proyectos por nombre......"
              value={searchProject}
              onChange={(e) => {
                setSeachProject(e.target.value);
              }}
            />
          </div>

          <div id="tabla-project" className="overflow-hidden rounded-md border">
            <DataTable columns={ProjectColum} data={proyectos} />
          </div>
        </div>
        <div className="flex flex-col gap-3">
          <h2>Tareas</h2>
          <div className="flex gap-3">
            <Input
              placeholder="Buscar tareas por nombre......"
              onChange={(e) => setSeachTask(e.target.value)}
            />
          </div>

          <div id="tabla-task" className="overflow-hidden rounded-md border">
            <DataTable columns={TaskColum} data={tareas} />
          </div>
        </div>
      </div>
    </div>
  );
}
