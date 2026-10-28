import { Input, DataTable } from "@/components/index.comp";
import { useState } from "react";
import { ProjectColum } from "@/components/common/tools/index.tool"
import Proyectos from "@/assets/tempdata/projects.json";

const PROYECTOS = Proyectos

export function DataTableProjects() {
  const [searchProject, setSeachProject] = useState("");

  return (
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
        <DataTable
          columns={ProjectColum}
          data={PROYECTOS}
          onClic={() => {}}
        />
      </div>
    </div>
  );
}
