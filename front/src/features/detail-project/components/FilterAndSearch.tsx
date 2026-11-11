import { Plus, Search, X } from "lucide-react";
import { Button, FilterSelector, Input } from "@/components";
import type { UserSummary } from "@/lib/api/types";
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

  const hayFiltros = Object.values(filters).some(Boolean) || busqueda !== "";

  // Cambia un solo filtro
  const set =
    <K extends keyof TaskFilters>(key: K) =>
    (value: TaskFilters[K]) =>
      onFiltersChange({ ...filters, [key]: value });

  return (
    <div className="flex w-full flex-wrap items-center gap-2">
      <div className="relative w-full sm:w-64">
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
        className="w-auto"
        items={TASK_STATUS_OPTIONS}
        value={filters.status}
        onChange={set("status")}
      />
      <FilterSelector
        label="Prioridad"
        placeholder="Todos"
        className="w-auto"
        items={TASK_PRIORITY_OPTIONS}
        value={filters.priority}
        onChange={set("priority")}
      />
      <FilterSelector
        label="Asignado"
        placeholder="Todos"
        className="w-auto"
        items={memberOptions}
        value={filters.member}
        onChange={set("member")}
      />

      {hayFiltros && (
        <Button
          variant="ghost"
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
