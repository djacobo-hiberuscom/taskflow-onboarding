// COMPONENTS
import { DeailTaskDrawer, BotoneraFooter, FiltersTask, HeaderTableTask, HeaderTask, RowTableTask } from "./components/index.comp";
// TYPES
import type { Task, TaskStatus, TaskPriority } from "#lib/api/types";

// CONST
import tareasJson from "@/assets/tempdata/tasks.json";
import PROYECTOS from "@/assets/tempdata/projects.json";
const TAREAS = tareasJson as Task[]

import { useState } from "react";
import { keycloak } from "#lib/auth/keycloak";
import { useTasks } from "#hooks/use-task";
import { ApiError } from "#lib/api/client";
import { NotFound } from "@/app/layouts";

// CONST
const Ntareas = TAREAS.length;
const Nproyectos = PROYECTOS.length;

// FEAT
export function TaskFeat() {
  

  // IS ADMIN
  const isAdmin = keycloak.hasRealmRole("Admin");
  // FILTERS
  const [projectFilter, setProjectFilter] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<TaskStatus | null>(null);
  const [priorityFilter, setPrirorityFilter] = useState<TaskPriority | null>(null);
  const [memberFilter, setMemberFilter] = useState<string | null>(null);
  // BUSCADOR
  const [buscador, setbuscador] = useState("");

  // Select task
  const [selectTask, setSelectTask] = useState<Task | null>(null);
  // PAGE
  const PAGE_SIZE = 10;
  const [page, setPage] = useState(1);

  // TAREAS
  const {data: tareasFiltradas, isLoading, error} = useTasks({
    projectId: projectFilter ?? undefined,
    status: statusFilter ? [statusFilter] : undefined,
    priority: priorityFilter ? [priorityFilter] : undefined,
    assigneeId: memberFilter ?? undefined,
    page,
    pageSize: PAGE_SIZE
  })
   if (isLoading) return <h1>Cargando</h1>;
    if (error instanceof ApiError && error.status === 404) return <NotFound />;
    if (error) return <h1>{error.message}</h1>;
    if (!tareasFiltradas) return <NotFound />;
  

  const totalPages = Math.ceil(Ntareas / PAGE_SIZE);

  // QUITAR FILTROS
  const quitarFiltros = () => {
    setProjectFilter(null);
    setStatusFilter(null);
    setPrirorityFilter(null);
    setMemberFilter(null);
    setbuscador("");
  };

  return (
    <div className="flex flex-col gap-8 justify-start p-3">
      <DeailTaskDrawer
        isAdmin={isAdmin}
        isOpen={selectTask !== null}
        tarea={selectTask}
        proyecto={PROYECTOS.find((p) => p.id === selectTask?.projectId)}
        onOpenChange={(open) => {
          if (!open) setSelectTask(null);
        }}
      />
      <HeaderTask Ntareas={Ntareas} Nproyectos={Nproyectos}/>
      <FiltersTask
        isAdmin={null}
        projectFilter={projectFilter}
        setProjectFilter={setProjectFilter}
        statusFilter={statusFilter}
        setStatusFilter={setStatusFilter}
        priorityFilter={priorityFilter}
        setPrirorityFilter={setPrirorityFilter}
        memberFilter={memberFilter}
        setMemberFilter={setMemberFilter}
        buscador={buscador}
        setbuscador={setbuscador}
        quitarFiltros={quitarFiltros}
        setPage={() => setPage(1)}
      />
      <div>
        <HeaderTableTask />
        <div id="Tabla" className=" flex-1">
          {tareasFiltradas.map((t) => {
            return (
              <RowTableTask
                tarea={t}
                onClictask={() => setSelectTask(t === selectTask ? null : t)}
              />
            );
          })}
        </div>
      </div>

      <BotoneraFooter
        nextClic={() => {
          if (page + 1 <= totalPages) setPage(page + 1);
        }}
        previousClic={() => {
          if (page - 1 > 0) setPage(page - 1);
        }}
        actualPage={page}
        totalPages={totalPages}
      />
    </div>
  );
}
