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
import type { TaskStatus, TaskPriority } from "#lib/api/types";

// CONST
import { ApiError } from "#lib/api/client";
import { NotFound } from "@/app/layouts/index";
import { FilterAndSearch } from "./components/FilterAndSearch";
import { useTask, useTasks } from "#hooks/use-task";

// COMPONENTE
export function DetailProject() {
  // CONST
  const nav = useNavigate();
  // PAGE
  const PAGE_SIZE = 10;
  const [page, setPage] = useState(1);
  //
  const { id } = useParams({ from: "/app/projects/$id" }); // ID del proyecto actual
  const { data, isLoading, error } = useProject(id);

  const [busqueda, setBusqueda] = useState("");
  const [status, setStatus] = useState<TaskStatus | null>(null);
  const [priority, setPriority] = useState<TaskPriority | null>(null);
  const [member, setMember] = useState<string | null>(null);

  const {
    data: tareasFiltradas,
    isLoading: tareasLoading,
    error: tareasError,
  } = useTasks({
    projectId: id ?? undefined,
    status: status?.toString ? [status] : undefined,
    priority: priority?.toString ? [priority] : undefined,
    assigneeId: member ?? undefined,
    page,
    pageSize: PAGE_SIZE,
  });

  const [selectTaskId, setSelectTaskId] = useState<string | null>(null);
  const { data: selectTask } = useTask(selectTaskId ?? undefined);
  const [popup, setPopup] = useState<string | null>(null);

  // Error stados
  if (isLoading) return <h1>Cargando</h1>;
  if (error instanceof ApiError && error.status === 404) return <NotFound />;
  if (error) return <h1>{error.message}</h1>;
  if (!data) return <NotFound />;

  if (tareasLoading) return <h1>Cargando</h1>;
  if (tareasError instanceof ApiError && tareasError.status === 404)
    return <NotFound />;
  if (tareasError) return <h1>{tareasError.message}</h1>;
  if (!tareasFiltradas) return <NotFound />;

  // OBtener proyecto
  const proyecto = data;

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
        isOpen={selectTaskId !== null}
        tarea={selectTask ?? null}
        proyecto={proyecto}
        onOpenChange={(open) => {
          if (!open) setSelectTaskId(null);
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
            data={tareasFiltradas.items}
            onClic={(t) => {
              setSelectTaskId(t.id === selectTaskId ? null : t.id);
            }}
          />
        </div>
      </div>
    </div>
  );
}
