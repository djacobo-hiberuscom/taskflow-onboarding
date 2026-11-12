import { Plus, Search, X } from "lucide-react";
import { Button, FilterSelector, Input } from "@/components";
import { useAssignableUsers } from "@/hooks/use-users";
import {
  TASK_PRIORITY_OPTIONS,
  TASK_STATUS_OPTIONS,
} from "@/lib/constants/task";
import { EMPTY_FILTERS, type TaskFilters } from "./types";

interface FilterAndSearchProps {
  filters: TaskFilters;
  onFiltersChange: (filters: TaskFilters) => void;
  busqueda: string;
  onBusquedaChange: (v: string) => void;
  onNewTask: () => void;
}

// Buscador + filtros + "Nueva tarea"
export function FilterAndSearch({
  filters,
  onFiltersChange,
  busqueda,
  onBusquedaChange,
  onNewTask,
}: FilterAndSearchProps) {
  const { data: usuarios = [] } = useAssignableUsers();
  
  const memberOptions = usuarios.map((u) => ({
    label: u.displayName,
    value: u.id,
  }));

  const hayFiltros = filters.status !== null || filters.priority !== null || filters.member !== null || busqueda !== "";

  const filterClasname = "w-auto bg-card shadow-sm";

  return (
    <div className="flex w-full flex-wrap items-center gap-2 bg-muted p-1 rounded-md">
      <div className="relative w-full sm:w-64 bg-card rounded-md overflow-hidden shadow-sm">
        <Search className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          value={busqueda}
          placeholder="Buscar por nombre..."
          className="pl-8"
          onChange={(e) => onBusquedaChange(e.target.value)}
        />
      </div>

      <FilterSelector
        label="Estado"
        placeholder="Todos"
        className={filterClasname}
        items={TASK_STATUS_OPTIONS}
        value={filters.status}
        onChange={(value) => onFiltersChange({...filters, status: value})}
      />
      <FilterSelector
        label="Prioridad"
        placeholder="Todos"
        className={filterClasname}
        items={TASK_PRIORITY_OPTIONS}
        value={filters.priority}
        onChange={(value) => onFiltersChange({...filters, priority: value})}
      />
      <FilterSelector
        label="Asignado"
        placeholder="Todos"
        className={filterClasname}
        items={memberOptions}
        value={filters.member}
        onChange={(value) => onFiltersChange({...filters, member: value})}
      />

      {hayFiltros && (
        <Button
          variant="destructive"
          size="sm"
          className="text-muted-foreground"
          onClick={() => {
            onFiltersChange(EMPTY_FILTERS);
            onBusquedaChange("");
          }}
        >
          <X />
          Limpiar
        </Button>
      )}

      <Button className="ml-auto" onClick={onNewTask}>
        <Plus />
        Nueva tarea
      </Button>
    </div>
  );
}
