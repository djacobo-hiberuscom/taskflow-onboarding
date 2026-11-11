import { X } from "lucide-react";
import { Button, FilterSelector, Input } from "@/components";
import { useAssignableUsers } from "@/hooks/use-users";
import type { Project } from "@/lib/api/types";
import {
  TASK_PRIORITY_OPTIONS,
  TASK_STATUS_OPTIONS,
} from "@/lib/constants/task";
import { EMPTY_FILTERS, type TaskFilters } from "./types";

interface FiltersTaskProps {
  proyectos: Project[];
  filters: TaskFilters;
  onFiltersChange: (filters: TaskFilters) => void;
  buscador: string;
  onBuscadorChange: (value: string) => void;
}

// Filtros + buscador
export function FiltersTask({
  proyectos,
  filters,
  onFiltersChange,
  buscador,
  onBuscadorChange,
}: FiltersTaskProps) {
  const { data: usuarios = [] } = useAssignableUsers();

  const projectOptions = proyectos.map((p) => ({ label: p.name, value: p.id }));
  const memberOptions = [
    { label: "Sin asignar", value: "unassigned" },
    ...usuarios.map((u) => ({ label: u.displayName, value: u.id })),
  ];

  // Cambia un solo filtro
  const set =
    <K extends keyof TaskFilters>(key: K) =>
    (value: TaskFilters[K]) =>
      onFiltersChange({ ...filters, [key]: value });

  const filterSelectorClasname = "rounded-md bg-background shadow-sm";
  return (
    <div className="flex gap-2 bg-muted p-1 rounded-lg items-center">
      <FilterSelector
        className={filterSelectorClasname}
        items={projectOptions}
        placeholder="Proyectos"
        value={filters.project}
        onChange={set("project")}
      />
      <FilterSelector
        className={filterSelectorClasname}
        items={TASK_STATUS_OPTIONS}
        placeholder="Estados"
        value={filters.status}
        onChange={set("status")}
      />
      <FilterSelector
        className={filterSelectorClasname}
        items={TASK_PRIORITY_OPTIONS}
        placeholder="Prioridad"
        value={filters.priority}
        onChange={set("priority")}
      />
      <FilterSelector
        className={filterSelectorClasname}
        items={memberOptions}
        placeholder="Miembros"
        value={filters.member}
        onChange={set("member")}
      />
      <Button
        className={filterSelectorClasname}
        variant="outline"
        onClick={() => {
          onFiltersChange(EMPTY_FILTERS);
          onBuscadorChange("");
        }}
      >
        <X />
        Quitar filtros
      </Button>
      <Input
        className={filterSelectorClasname}
        value={buscador}
        onChange={(e) => onBuscadorChange(e.target.value)}
        placeholder="Buscar por nombre"
      >
      </Input>
    </div>
  );
}
