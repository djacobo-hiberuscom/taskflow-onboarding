import { DataTable } from "@/components/index.comp";
import { ProjectColum, TaskColum } from "@/app/layouts/app/components/index.comp";
import Proyectos from "@/assets/tempdata/projects.json"
import Tareas from "@/assets/tempdata/tasks.json"


const proyectos = Proyectos
const tareas = Tareas


export function DashboardFeat() {
  return (
    <div className="p-5">
        <div className="flex flex-col gap-5">
          <div id="tabla-project" className="overflow-hidden rounded-md border">
            <DataTable columns={ProjectColum} data={proyectos} />
          </div>
          <div>
            <DataTable columns={TaskColum} data={tareas}/>
          </div>
        </div>
    </div>
  );
}
