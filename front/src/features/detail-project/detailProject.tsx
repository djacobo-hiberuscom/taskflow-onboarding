import { useState } from "react";
import { useParams } from "@tanstack/react-router";
import { DataTable, QueryState, TaskDetailDrawer } from "@/components/index.comp";
import { useProject } from "@/hooks/use-project";
import { useTasks } from "@/hooks/use-task";
import {
  BackButton,
  EMPTY_FILTERS,
  FilterAndSearch,
  Header,
  PopupContainer,
  TaskColum,
  type DetailProjectPopup,
  type TaskFilters,
} from "./components";

// Máximo permitido por el back; la DataTable pagina y ordena en cliente
const MAX_PAGE_SIZE = 100;

// FEAT: Detalle de proyecto
export function DetailProjectFeat() {
  const { id } = useParams({ from: "/app/projects/$id" });

  const [popup, setPopup] = useState<DetailProjectPopup | null>(null);
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(null);
  const [filters, setFilters] = useState<TaskFilters>(EMPTY_FILTERS);
  const [busqueda, setBusqueda] = useState("");

  const project = useProject(id);
  const tasks = useTasks({
    projectId: id,
    status: filters.status ? [filters.status] : undefined,
    priority: filters.priority ? [filters.priority] : undefined,
    assigneeId: filters.member ?? undefined,
    pageSize: MAX_PAGE_SIZE,
  });

  if (project.isLoading || project.error || !project.data)
    return <QueryState isLoading={project.isLoading} error={project.error} />;

  const proyecto = project.data;
  // Búsqueda por nombre en cliente
  const tareas = (tasks.data?.items ?? []).filter((t) =>
    t.title.toLowerCase().includes(busqueda.trim().toLowerCase()),
  );

  return (
    <div>
      <TaskDetailDrawer taskId={selectedTaskId} onClose={() => setSelectedTaskId(null)} />
      <PopupContainer proyecto={proyecto} popup={popup} close={() => setPopup(null)} />

      <BackButton />

      <div className="flex flex-col bg-background items-center justify-center gap-8 p-5 m-5 border rounded-xl">
        <Header proyecto={proyecto} open={setPopup} />

        <div className="flex flex-col overflow-hidden rounded-md bg-background shadow-2xs p-3 gap-4 w-full">
          <FilterAndSearch
            filters={filters}
            onFiltersChange={setFilters}
            busqueda={busqueda}
            onBusquedaChange={setBusqueda}
            miembros={proyecto.members}
          />

          {tasks.isLoading || tasks.error ? (
            <QueryState isLoading={tasks.isLoading} error={tasks.error} />
          ) : (
            <DataTable
              columns={TaskColum}
              data={tareas}
              onClic={(t) => setSelectedTaskId(t.id === selectedTaskId ? null : t.id)}
            />
          )}
        </div>
      </div>
    </div>
  );
}
