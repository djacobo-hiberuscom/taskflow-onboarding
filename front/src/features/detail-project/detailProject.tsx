// router
import { useNavigate, useParams } from "@tanstack/react-router";

// HOOKS
import { useState } from "react";
import { useProject } from "@/hooks/use-project";

// COMPONENTES
import { DataTable } from "@/components/index.comp";
import {
  TaskColum,
  BackButton,
  DeailTaskDrawer,
  PopupContainer,
  Header,
} from "./components/index";

// TYPES
import type { Task } from "#lib/api/types";

// CONST
import tareasJson from "@/assets/tempdata/tasks.json";
const TAREAS = tareasJson as Task[]

import { ApiError } from "#lib/api/client";
import { NotFound } from "@/app/layouts/index";
import { FilterAndSearch } from "./components/FilterAndSearch";

// COMPONENTE
export function DetailProject() {
  // CONST
  const nav = useNavigate();
  // 
  const { id } = useParams({ from: "/app/projects/$id" }); // ID del proyecto actual
  const { data, isLoading, error } = useProject(id);
  const [selectTask, setSelectTask] = useState<Task | null>(null); // Tarea selecionada para el drawer
  const [popup, setPopup] = useState<string | null>(null);

  const [busqueda, setBusqueda] = useState("");
  const [status, setStatus] = useState<string | null>(null);
  const [priority, setPriority] = useState<string | null>(null);
  const [member, setMember] = useState<string | null>(null);

  // Error stados
  if (isLoading) return <h1>Cargando</h1>;
  if (error instanceof ApiError && error.status === 404) return <NotFound />;
  if (error) return <h1>{error.message}</h1>;
  if (!data) return <NotFound />;

  // OBtener proyecto
  const proyecto = data;

  // TAREAS DEL PROYECTO
  const projectTask = TAREAS.filter((t) => t.projectId === id);

  // Filtrar tareas
  const filterTask = projectTask.filter(
    (t) =>
      (!status || t.status === status) &&
      (!priority || t.priority === priority) &&
      (!member || t.assignee?.id === member) &&
      t.title.toLowerCase().startsWith(busqueda.toLowerCase()),
  );

  // NAV atras
  const navBack = () => {
    nav({
      to: "/projects",
    });
  };

  // COMPONENTE
  return (
    <div>
      <DeailTaskDrawer
        isOpen={selectTask !== null}
        tarea={selectTask}
        proyecto={proyecto}
        onOpenChange={(open) => {
          if (!open) setSelectTask(null);
        }}
      />
      <PopupContainer
        proyecto={proyecto}
        popup={popup}
        close={() => setPopup(null)}
      />

      <BackButton navBack={navBack} />

      <div className="flex flex-col bg-background items-center justify-center gap-8 p-5 m-5 border rounded-xl">
        <Header
          proyecto={proyecto}
          editClic={() => setPopup("Edit")}
          archiveClic={() => setPopup("Archive")}
          deleteClic={() => setPopup("Delete")}
          createClic={() => setPopup("Create")}
        />
        <div
          id="tabla-task"
          className="flex flex-col overflow-hidden rounded-md bg-background shadow-2xs p-3 gap-4 w-full"
        >
          <FilterAndSearch
            busqueda={busqueda}
            member={member}
            miembros={proyecto.members}
            status={status}
            priority={priority}
            onBusquedaChange={setBusqueda}
            onMemberChange={setMember}
            onStatuseChange={setStatus}
            onPriorityChange={setPriority}
          />

          <DataTable
            columns={TaskColum}
            data={filterTask}
            onClic={(t) => {
              setSelectTask(t === selectTask ? null : t);
            }}
          />
        </div>
      </div>
    </div>
  );
}
