import { useState } from "react";
import { Pagination, QueryState, TaskDetailDrawer } from "@/components";
import { useProjects } from "@/hooks/use-project";
import { useTasks } from "@/hooks/use-task";
import {
  EMPTY_FILTERS,
  FiltersTask,
  HeaderTableTask,
  HeaderTask,
  RowTableTask,
  type TaskFilters,
} from "./components/index.comp";

const PAGE_SIZE = 10;

// FEAT: Tareas
export function TaskFeat() {
  const [filters, setFilters] = useState<TaskFilters>(EMPTY_FILTERS);
  const [buscador, setBuscador] = useState("");
  const [page, setPage] = useState(1);
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(null);

  const projects = useProjects({ pageSize: 100 });
  const tasks = useTasks({
    projectId: filters.project ?? undefined,
    status: filters.status ? [filters.status] : undefined,
    priority: filters.priority ? [filters.priority] : undefined,
    assigneeId: filters.member ?? undefined,
    page,
    pageSize: PAGE_SIZE,
  });

  // Al cambiar filtros se vuelve a la primera página
  const changeFilters = (next: TaskFilters) => {
    setFilters(next);
    setPage(1);
  };

  const loading = projects.isLoading || tasks.isLoading;
  const error = projects.error ?? tasks.error;
  if (loading || error) return <QueryState isLoading={loading} error={error} />;

  const totalTareas = tasks.data?.totalCount ?? 0;
  const totalPages = Math.ceil(totalTareas / PAGE_SIZE);
  // Búsqueda por nombre en cliente (sobre la página actual)
  const tareas = (tasks.data?.items ?? []).filter((t) =>
    t.title.toLowerCase().includes(buscador.trim().toLowerCase()),
  );

  return (
    <div className="flex flex-1 flex-col gap-8 justify-start p-3">
      <TaskDetailDrawer
        taskId={selectedTaskId}
        onClose={() => setSelectedTaskId(null)}
      />

      <HeaderTask
        nTareas={totalTareas}
        nProyectos={projects.data?.totalCount ?? 0}
      />

      <FiltersTask
        proyectos={projects.data?.items ?? []}
        filters={filters}
        onFiltersChange={changeFilters}
        buscador={buscador}
        onBuscadorChange={setBuscador}
      />

      <div>
        <HeaderTableTask />
        {tareas.length === 0 ? (
          <p className="p-6 text-center text-muted-foreground bg-background">
            Sin resultados
          </p>
        ) : (
          tareas.map((t) => (
            <RowTableTask
              key={t.id}
              tarea={t}
              onClick={() =>
                setSelectedTaskId(t.id === selectedTaskId ? null : t.id)
              }
            />
          ))
        )}
      </div>

      <Pagination
        className="relateive z-10 sticky bottom-5 mt-auto flex justify-end items-center gap-2"
        page={page}
        totalPages={totalPages}
        onPageChange={setPage}
      />
    </div>
  );
}
