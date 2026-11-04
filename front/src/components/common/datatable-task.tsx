import { Input, DataTable } from "@/components/index.comp";
import { useState } from "react";
import { TaskColum } from "@/components/common/tools/index.tool"
import Tareas from "@/assets/tempdata/tasks.json";

const TAREAS = Tareas

export function DataTableTask() {
  const [searchTask, setSeachTask] = useState("");
  

  return (
    <div className="flex flex-col gap-3">
      <h2>Tareas</h2>
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
