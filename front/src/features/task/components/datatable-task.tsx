import { Input, DataTable } from "@/components/index.comp";
import { useState } from "react";
import { TaskColum } from "@/features/projects/components/colum-task"
import Tareas from "@/assets/tempdata/tasks.json";

const TAREAS = Tareas

export function DataTableTask() {
  const [searchTask, setSeachTask] = useState("");
  

  return (
    <div className="flex flex-col gap-3">
      <div className="flex gap-3">
        <Input
          placeholder="Buscar proyectos por nombre......"
          value={searchTask
}
          onChange={(e) => {
            setSeachTask(e.target.value);
          }}
        />
      </div>

      <div id="tabla-project" className="overflow-hidden rounded-md border">
        <DataTable
          columns={TaskColum}
          data={TAREAS}
          onClic={() => {}}
        />
      </div>
    </div>
  );
}
