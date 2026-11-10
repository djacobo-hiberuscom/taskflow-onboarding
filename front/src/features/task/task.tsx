// COMPONENTS
import {
  DeailTaskDrawer,
  BotoneraFooter,
  FiltersTask,
  HeaderTableTask,
  HeaderTask,
  RowTableTask,
} from "./components/index.comp";
// TYPES
import type { TaskStatus, TaskPriority } from "@/lib/api/types";

// CONST

import { useState } from "react";
import { keycloak } from "#lib/auth/keycloak";
import { useTasks, useTask } from "#hooks/use-task";
import { ApiError } from "#lib/api/client";
import { NotFound } from "@/app/layouts";
import { useProjects } from "#hooks/use-project";

// FEAT
export function TaskFeat() {
  // IS ADMIN
  const isAdmin = keycloak.hasRealmRole("Admin");
  // FILTERS
  const [projectFilter, setProjectFilter] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<TaskStatus | null>(null);
  const [priorityFilter, setPrirorityFilter] = useState<TaskPriority | null>(
    null,
  );
  const [memberFilter, setMemberFilter] = useState<string | null>(null);
  // BUSCADOR
  const [buscador, setbuscador] = useState("");

  // Select task
  const [selectTaskId, setSelectTaskId] = useState<string | null>(null);
  const { data: selectTask } = useTask(selectTaskId ?? undefined);
  // PAGE
  const PAGE_SIZE = 10;
  const [page, setPage] = useState(1);
  // PROYECTOS
  const {
    data: proyectos,
    isLoading: proyectosLoading,
    error: proyectosError,
  } = useProjects(); // Proyectos

  // TAREAS
  const {
    data: tareasFiltradas,
    isLoading: tareasLoading,
    error: tareasError,
  } = useTasks({
    projectId: projectFilter ?? undefined,
    status: statusFilter ? [statusFilter] : undefined,
    priority: priorityFilter ? [priorityFilter] : undefined,
    assigneeId: memberFilter ?? undefined,
    page,
    pageSize: PAGE_SIZE,
  });
  if (proyectosLoading) return <h1>Cargando</h1>;
  if (proyectosError instanceof ApiError && proyectosError.status === 404)
    return <NotFound />;
  if (proyectosError) return <h1>{proyectosError.message}</h1>;
  if (!proyectos) return <NotFound />;
  if (tareasLoading) return <h1>Cargando</h1>;
  if (tareasError instanceof ApiError && tareasError.status === 404)
    return <NotFound />;
  if (tareasError) return <h1>{tareasError.message}</h1>;
  if (!tareasFiltradas) return <NotFound />;
  // CONST
  const Ntareas = tareasFiltradas.items.length;
  const Nproyectos = proyectos.items.length;

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
    <div className="flex flex-1 flex-col gap-8 justify-start p-3">
      <DeailTaskDrawer
        isAdmin={isAdmin}
        isOpen={selectTaskId !== null}
        tarea={selectTask}
        proyecto={selectTask?.projectId}
        onOpenChange={(open) => {
          if (!open) setSelectTaskId(null);
        }}
      />
      <HeaderTask Ntareas={Ntareas} Nproyectos={Nproyectos} />
      <FiltersTask
        itemProject={proyectos.items}
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
      <div className="">
        <HeaderTableTask />
        <div id="Tabla" className=" flex-1">
          {tareasFiltradas.items.map((t) => {
            return (
              <RowTableTask
                tarea={t}
                onClictask={() => setSelectTaskId(t.id === selectTaskId ? null : t.id)}
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
