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

  const hayFiltros =
    filters.member !== null ||
    filters.status !== null ||
    filters.priority !== null ||
    filters.project !== null ||
    buscador !== "";

  const filterSelectorClasname = "rounded-md bg-card shadow-sm";
  return (
    <div className="flex gap-2 bg-muted p-1 rounded-lg items-center">
      <FilterSelector
        className={filterSelectorClasname}
        items={projectOptions}
        placeholder="Proyectos"
        value={filters.project}
        onChange={(value) => onFiltersChange({ ...filters, project: value })}
      />
      <FilterSelector
        className={filterSelectorClasname}
        items={TASK_STATUS_OPTIONS}
        placeholder="Estados"
        value={filters.status}
        onChange={(value) => onFiltersChange({ ...filters, status: value })}
      />
      <FilterSelector
        className={filterSelectorClasname}
        items={TASK_PRIORITY_OPTIONS}
        placeholder="Prioridad"
        value={filters.priority}
        onChange={(value) => onFiltersChange({ ...filters, priority: value })}
      />
      <FilterSelector
        className={filterSelectorClasname}
        items={memberOptions}
        placeholder="Miembros"
        value={filters.member}
        onChange={(value) => onFiltersChange({ ...filters, member: value })}
      />
      {hayFiltros && (
        <Button
          className={filterSelectorClasname}
          variant="destructive"
          onClick={() => {
            onFiltersChange(EMPTY_FILTERS);
            onBuscadorChange("");
          }}
        >
          <X />
          Quitar filtros
        </Button>
      )}
      <Input
        className={filterSelectorClasname}
        value={buscador}
        onChange={(e) => onBuscadorChange(e.target.value)}
        placeholder="Buscar por nombre"
      ></Input>
    </div>
  );
}
