import { X } from "lucide-react";
import { Button, FilterSelector, Input } from "@/components/index.comp";
import type { UserSummary } from "@/lib/api/types";
import { TASK_PRIORITY_OPTIONS, TASK_STATUS_OPTIONS } from "@/lib/constants/task";
import { EMPTY_FILTERS, type TaskFilters } from "./types";

interface FilterAndSearchProps {
  filters: TaskFilters;
  onFiltersChange: (filters: TaskFilters) => void;
  busqueda: string;
  onBusquedaChange: (v: string) => void;
  miembros: UserSummary[];
}

// Filtros + buscador de la tabla de tareas del proyecto
export function FilterAndSearch({
  filters,
  onFiltersChange,
  busqueda,
  onBusquedaChange,
  miembros,
}: FilterAndSearchProps) {
  const memberOptions = miembros.map((m) => ({ label: m.displayName, value: m.id }));

  // Cambia un solo filtro
  const set = <K extends keyof TaskFilters>(key: K) => (value: TaskFilters[K]) =>
    onFiltersChange({ ...filters, [key]: value });

  return (
    <div className="flex gap-2 items-center">
      <FilterSelector
        items={TASK_STATUS_OPTIONS}
        placeholder="Selecciona estado"
        value={filters.status}
        onChange={set("status")}
      />
      <FilterSelector
        items={TASK_PRIORITY_OPTIONS}
        placeholder="Selecciona prioridad"
        value={filters.priority}
        onChange={set("priority")}
      />
      <FilterSelector
        items={memberOptions}
        placeholder="Selecciona miembro"
        value={filters.member}
        onChange={set("member")}
      />
      <Button variant="ghost" onClick={() => onFiltersChange(EMPTY_FILTERS)}>
        <X />
        Quitar filtros
      </Button>
      <Input
        value={busqueda}
        placeholder="Buscar por nombre"
        onChange={(e) => onBusquedaChange(e.target.value)}
      />
    </div>
  );
}
