// componentes
import {Button, Input} from "@/components/index.comp"
import { FilterSeletor } from "./index.comp";
// ICON
import { X } from "lucide-react";

//
import USUARIOS from "@/assets/tempdata/users.json"
import PROYECTOS from "@/assets/tempdata/projects.json"
import type { TaskStatus } from "#lib/api/types";

// Filters
const project_filter = PROYECTOS.map((p) => ({
  // PROYECTOS
  label: p.name,
  value: p.id,
}));
const status_filter = [
  // ESTADO
  { label: "Hecho", value: "Done" },
  { label: "Sin hacer", value: "To Do" },
  { label: "En proceso", value: "In Progress" },
];
const priority_filter = [
  // PRIORIDAD
  { label: "Alta", value: "High" },
  { label: "Madia", value: "Medium" },
  { label: "Baja", value: "Low" },
];
const usuarios_filter = USUARIOS.map((u) => ({
  // USUARIOS
  label: u.display_name,
  value: u.id,
}));
interface filterParams {
  isAdmin: boolean | null;
  projectFilter: string | null;
  setProjectFilter: (value: string | null) => void;
  statusFilter: string | null;
  setStatusFilter: (value: TaskStatus | null) => void;
  priorityFilter: string | null;
  setPrirorityFilter: (value: TaskPriority | null) => void;
  memberFilter: string | null;
  setMemberFilter: (value: string | null) => void;
  buscador: string;
  setbuscador: (value: string) => void;
  quitarFiltros: () => void;
  setPage: (value: number) => void;
}
export function FiltersTask({
  isAdmin,
  projectFilter,
  setProjectFilter,
  statusFilter,
  setStatusFilter,
  priorityFilter,
  setPrirorityFilter,
  memberFilter,
  setMemberFilter,
  buscador,
  setbuscador,
  quitarFiltros,
  setPage,
}: filterParams) {
  // Componente

  return (
    <div id="Filtros" className="flex gap-2">
      <FilterSeletor
        isAdmin={isAdmin}
        items={project_filter}
        placeholder="Proyectos"
        value={projectFilter}
        onChange={setProjectFilter}
      />
      <FilterSeletor
        isAdmin={isAdmin}
        items={status_filter}
        placeholder="Estados"
        value={statusFilter}
        onChange={setStatusFilter}
      />
      <FilterSeletor
        isAdmin={isAdmin}
        items={priority_filter}
        placeholder="Priorida"
        value={priorityFilter}
        onChange={setPrirorityFilter}
      />
      <FilterSeletor
        isAdmin={isAdmin}
        items={usuarios_filter}
        placeholder="Miembros"
        value={memberFilter}
        onChange={setMemberFilter}
      />
      <Button onClick={quitarFiltros} variant="outline">
        <X />
        Quitar filtros
      </Button>
      <Input
        value={buscador}
        onChange={(e) => {
          setbuscador(e.target.value);
          setPage(1);
        }}
        placeholder="Buscar por nombre"
      />
    </div>
  );
}