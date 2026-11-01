// ICONOS
import { X } from "lucide-react";
// popups
import {} from "./popups/index.popup";
// COMPONENTS
import { Badge } from "@/components/index.comp";
import { DeailTaskDrawer } from "./components/index.comp";
// TYPES
import type { Task } from "#lib/api/types";

// CONST
import TAREAS from "@/assets/tempdata/tasks.json";
import PROYECTOS from "@/assets/tempdata/projects.json";
import USUARIOS from "@/assets/tempdata/users.json";
import { FilterSeletor } from "./components/index.comp";
import { Button, Input } from "@/components/index.comp";
import { useState } from "react";
import { keycloak } from "#lib/auth/keycloak";

// CONST
const Ntareas = TAREAS.length;
const Nproyectos = PROYECTOS.length;

// FEAT
export function TaskFeat() {
  // IS ADMIN
  const isAdmin = keycloak.hasRealmRole("Admin");
  // FILTERS
  const [projectFilter, setProjectFilter] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<string | null>(null);
  const [priorityFilter, setPrirorityFilter] = useState<string | null>(null);
  const [memberFilter, setMemberFilter] = useState<string | null>(null);
  // BUSCADOR
  const [buscador, setbuscador] = useState("");

  // Select task
  const [selectTask, setSelectTask] = useState<Task | null>(null);
  // PAGE
  const PAGE_SIZE = 10;
  const [page, setPage] = useState(1);

  // TAREAS
  const tareasFiltradas = TAREAS.filter(
    (t) =>
      (!projectFilter || t.project_id === projectFilter) &&
      (!statusFilter || t.status === statusFilter) &&
      (!priorityFilter || t.priority === priorityFilter) &&
      (!memberFilter || t.assigned_id === memberFilter) &&
      t.title.toLocaleLowerCase().includes(buscador.toLocaleLowerCase()),
  ).slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  const totalPages = Math.ceil(Ntareas / PAGE_SIZE);

  // QUITAR FILTROS
  const quitarFiltros = () => {
    setProjectFilter(null);
    setStatusFilter(null);
    setPrirorityFilter(null);
    setMemberFilter(null);
    setbuscador("");
  };

  return (
    <div className="flex flex-col gap-8 justify-start p-3">
      <DeailTaskDrawer
        isAdmin={isAdmin}
        isOpen={selectTask !== null}
        tarea={selectTask}
        proyecto={PROYECTOS.find((p) => p.id === selectTask?.project_id)}
        onOpenChange={(open) => {
          if (!open) setSelectTask(null);
        }}
      />
      <HeaderTaskFeat />
      <FiltersTaskFeat
        isAdmin={null}
        projectFilter={projectFilter}
        setProjectFilter={setProjectFilter}
        statusFilter={statusFilter}
        setStatusFilter={setStatusFilter}
        priorityFilter={priorityFilter}
        setPrirorityFilter={setPrirorityFilter}
        memberFilter={memberFilter}
        setMemberFilter={setMemberFilter}
        buscador={buscador}
        setbuscador={setbuscador}
        quitarFiltros={quitarFiltros}
        setPage={() => setPage(1)}
      />
      <div>
        <HeaderTableTask />
        <div id="Tabla" className=" flex-1">
          {tareasFiltradas.map((t) => {
            return (
              <RowTableTask
                tarea={t}
                onClictask={() => setSelectTask(t === selectTask ? null : t)}
              />
            );
          })}
        </div>
      </div>

      <BotoneraFooter
        nextClic={() => {
          if (page + 1 <= totalPages) setPage(page + 1);
        }}
        previousClic={() => {
          if (page - 1 > 0) setPage(page - 1);
        }}
        actualPage={page}
        totalPages={totalPages}
      />
    </div>
  );
}

// header
function HeaderTaskFeat() {
  return (
    <div id="Header" className="">
      <h1 className="text-xs text-gray-500">
        {Ntareas} Tareas en {Nproyectos} Proyectos
      </h1>
    </div>
  );
}
// ------

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
  setStatusFilter: (value: string | null) => void;
  priorityFilter: string | null;
  setPrirorityFilter: (value: string | null) => void;
  memberFilter: string | null;
  setMemberFilter: (value: string | null) => void;
  buscador: string;
  setbuscador: (value: string) => void;
  quitarFiltros: () => void;
  setPage: (value: number) => void;
}
function FiltersTaskFeat({
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
// ------

// Header Table
function HeaderTableTask() {
  return (
    <div
      id="HeaderTable"
      className="grid grid-cols-6 items-center w-full text-gray-400 border rounded-t-2xl p-5"
    >
      <span>Nombre</span>
      <span>Proyecto</span>
      <span>Estado</span>
      <span>Prioridad</span>
      <span>Asignado</span>
      <span>vencimiento</span>
    </div>
  );
}
// ------
// TableRow
interface rowParams {
  tarea: Task;
  onClictask: () => void;
}
function RowTableTask({ tarea, onClictask }: rowParams) {
  return (
    <div
      className="grid grid-cols-6 justify-between w-full bg-background p-4 border-b gap-3 hover:brightness-95"
      onClick={onClictask}
    >
      <span className="truncate">{tarea.title}</span>
      <span className="truncate">{tarea.project_id}</span>
      <Badge className={statusClass(tarea.status)}>{tarea.status}</Badge>
      <Badge className={priorityClass(tarea.priority)}>{tarea.priority}</Badge>
      <span className="truncate">{tarea.assigned_id}</span>
      <span>{tarea.due_date}</span>
    </div>
  );
}

function priorityClass(prioridad?: Task["priority"]) {
  switch (prioridad) {
    case "High":
      return "bg-[#FEE2E2] text-[#991B1B]";
    case "Medium":
      return "bg-[#FEF3C7] text-[#92400E]";
    case "Low":
      return "bg-[#DCFCE7] text-[#166534]";
    default:
      return "";
  }
}
function statusClass(ststus?: Task["status"]) {
  switch (ststus) {
    case "Done":
      return "bg-[#DCFCE7] text-[#166534]";
    case "To Do":
      return "bg-[#FEF3C7] text-[#92400E]";
    case "In Progress":
      return "bg-[#DBEAFE] text-[#1E40AF]";
    default:
      return "";
  }
}

// ----
// BOTONOERA
interface botonParameters {
  nextClic: () => void;
  previousClic: () => void;
  actualPage: number;
  totalPages: number;
}
function BotoneraFooter({
  nextClic,
  previousClic,
  actualPage,
  totalPages,
}: botonParameters) {
  return (
    <div className="flex gap-3 items-center justify-end">
      <Button onClick={previousClic} variant="outline">
        Anterior
      </Button>
      <span>
        {actualPage} / {totalPages}
      </span>
      <Button onClick={nextClic} variant="outline">
        Siguiente
      </Button>
    </div>
  );
}
