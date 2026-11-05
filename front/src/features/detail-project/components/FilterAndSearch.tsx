// ICONS
import { X } from "lucide-react";

// Componentes
import { FilterSeletor } from "./index";
import { Button, Input } from "@/components/index.comp";
// Types
import type { UserSummary } from "@/lib/api/types";


// parametros del componente
interface params {
  busqueda: string;
  onBusquedaChange: (v: string) => void;
  status: string | null;
  onStatuseChange: (v: string | null) => void;
  priority: string | null;
  onPriorityChange: (v: string | null) => void;
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
    { label: "Sin hacer", value: "To Do" },
    { label: "En proceso", value: "In Progress" },
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
        onChange={onStatuseChange}
      />
      <FilterSeletor
        items={PRIORIDAD}
        placeholder="Seleciona prioridad"
        value={priority}
        onChange={onPriorityChange}
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
