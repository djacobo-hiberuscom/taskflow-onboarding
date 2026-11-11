import { useState } from "react";
import { useParams } from "@tanstack/react-router";
import { DataTable, QueryState, TaskDetailDrawer } from "@/components";
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
    <div className="relative">
      <TaskDetailDrawer
        taskId={selectedTaskId}
        onClose={() => setSelectedTaskId(null)}
      />
      <PopupContainer
        proyecto={proyecto}
        popup={popup}
        close={() => setPopup(null)}
      />

      <BackButton />

      <div className="mx-auto flex w-full max-w-7xl flex-col gap-6 p-6">
        <Header proyecto={proyecto} open={setPopup} />

        <FilterAndSearch
          filters={filters}
          onFiltersChange={setFilters}
          busqueda={busqueda}
          onBusquedaChange={setBusqueda}
          onNewTask={() => setPopup("create-task")}
        />

        {tasks.isLoading || tasks.error ? (
          <QueryState isLoading={tasks.isLoading} error={tasks.error} />
        ) : (
          <DataTable
            columns={TaskColum}
            data={tareas}
            itemLabel="tareas"
            onClic={(t) =>
              setSelectedTaskId(t.id === selectedTaskId ? null : t.id)
            }
          />
        )}
      </div>
    </div>
  );
}
