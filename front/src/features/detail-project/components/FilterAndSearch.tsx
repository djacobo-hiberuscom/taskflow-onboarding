// ICONS
import { X } from "lucide-react";

// Componentes
import { FilterSeletor } from "./index";
import { Button, Input } from "@/components/index.comp";
// Types
import type { UserSummary, TaskPriority, TaskStatus } from "@/lib/api/types";


// parametros del componente
interface params {
  busqueda: string;
  onBusquedaChange: (v: string) => void;
  status: string | null;
  onStatuseChange: (v: TaskStatus | null) => void;
  priority: string | null;
  onPriorityChange: (v: TaskPriority | null) => void;
  member: string | null;
  onMemberChange: (v: string | null) => void;
  miembros: UserSummary[];
}

// Componente
export function FilterAndSearch({
  busqueda,
  onBusquedaChange,
  status,
  onStatuseChange,
  priority,
  onPriorityChange,
  member,
  onMemberChange,
  miembros,
}: params) {

  // FILTROS
  const ESTADO = [
    { label: "Hecho", value: "Done" },
    { label: "Sin hacer", value: "ToDo" },
    { label: "En proceso", value: "InProgress" },
  ];
  const PRIORIDAD = [
    { label: "Alta", value: "High" },
    { label: "Madia", value: "Medium" },
    { label: "Baja", value: "Low" },
  ];
  const MIEMBROS = miembros.map((m) => ({
    label: m.displayName,
    value: m.id,
  }));

  // DESACTIVAR FILTROS
  const quitarFilters = () => {
    onStatuseChange(null)
    onMemberChange(null)
    onPriorityChange(null)
  };

  return (
    <div className="flex gap-2 items-center">
      <FilterSeletor
        items={ESTADO}
        placeholder="Seleciona estados"
        value={status}
        onChange={(v) => onStatuseChange(v as TaskStatus | null)}
      />
      <FilterSeletor
        items={PRIORIDAD}
        placeholder="Seleciona prioridad"
        value={priority}
        onChange={(v) => onPriorityChange(v as TaskPriority | null)}
      />
      <FilterSeletor
        items={MIEMBROS}
        placeholder="Seleciona miembros"
        value={member}
        onChange={onMemberChange}
      />
      <Button
        variant="ghost"
        className="flex justify-center items-center"
        onClick={quitarFilters}
      >
        <X />
        Quitar filtro
      </Button>
      <Input
        value={busqueda}
        placeholder="Buscar por nombre"
        onChange={(e) => onBusquedaChange(e.target.value)}
      />
    </div>
  );
}
