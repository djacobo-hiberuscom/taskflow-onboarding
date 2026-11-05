import { getUsernameById, getUsernameByProjectId } from "@/tools/getUsername";
import { useNavigate, useParams } from "@tanstack/react-router";

// HOOKS
import { useState } from "react";
import { useProject } from "#hooks/use-project";

// COMPONENTES
import {
  Input,
  DataTable,
  Button,
  Separator,
  Badge,
} from "@/components/index.comp";
import {
  FilterSeletor,
  TaskColum,
  BackButton,
  DeailTaskDrawer,
  PopupContainer,
} from "./components/index";

// ICONS
import { Pen, Archive, Trash, Plus, CircleUserRound, X } from "lucide-react";

// TYPES
import type { Task } from "#lib/api/types";

// CONST
import TAREAS from "@/assets/tempdata/tasks.json";
import { ApiError } from "#lib/api/client";
import { NotFound } from "@/app/layouts/index";

export function DetailProject() {
  // CONST
  const nav = useNavigate();
  const { id } = useParams({ from: "/app/projects/$id" }); // ID del proyecto actual
  const { data, isLoading, error } = useProject(id);

  const [selectTask, setSelectTask] = useState<Task | null>(null); // Tarea selecionada para el drawer
  const [busqueda, setBusqueda] = useState(""); // Buscador del input
  const [status, setStatus] = useState<string | null>(null); // selecion del filtro (status)
  const [priority, setPriority] = useState<string | null>(null); // Selecion del filtro (priority)
  const [member, setMember] = useState<string | null>(null); // selecion del filtro (miembros)

  const [popup, setPopup] = useState<string | null>(null);

  // Error stados
  if (isLoading) return <h1>Cargando</h1>;
  if (error instanceof ApiError && error.status === 404) return <NotFound />;
  if (error) return <h1>{error.message}</h1>;

  // OBtener proyecto
  const proyecto = data;

  // TAREAS DEL PROYECTO
  const projectTask = TAREAS.filter((t) => t.project_id === id);

  // Filtrar tareas
  const filterTask = projectTask.filter(
    (t) =>
      (!status || t.status === status) &&
      (!priority || t.priority === priority) &&
      (!member || t.assigned_id === member) &&
      t.title.toLowerCase().startsWith(busqueda.toLowerCase()),
  );

  // NAV atras
  const navBack = () => {
    nav({
      to: "/projects",
    });
  };

  // FILTROS
  const item_f1 = [
    { label: "Hecho", value: "Done" },
    { label: "Sin hacer", value: "To Do" },
    { label: "En proceso", value: "In Progress" },
  ];
  const item_f2 = [
    { label: "Alta", value: "High" },
    { label: "Madia", value: "Medium" },
    { label: "Baja", value: "Low" },
  ];
  const item_f3 = getUsernameByProjectId(id).map((iid) => ({
    label: getUsernameById(iid) ?? "SinNombre",
    value: iid,
  }));

  // DESACTIVAR FILTROS
  const quitarFilters = () => {
    setStatus(null);
    setPriority(null);
    setMember(null);
  };

  // COMPONENTE
  return (
    <div>
      <DeailTaskDrawer
        isOpen={selectTask !== null}
        tarea={selectTask}
        proyecto={proyecto}
        onOpenChange={(open) => {
          if (!open) setSelectTask(null);
        }}
      />
      <PopupContainer proyecto={proyecto} popup={popup} close={() => setPopup(null)}/>

      <BackButton navBack={navBack} />

      <div className="flex flex-col bg-background items-center justify-center gap-8 p-5 m-5 border rounded-xl">
        <div className="p-5 w-full flex justify-between">
          <div id="texto">
            <h1 className="text-3xl font-bold">{proyecto?.name}</h1>
            <p> {proyecto?.description}</p>

            <Badge className="mt-9 text-2xs font-bold text-[#166534] py-2 bg-[#DCFCE7]">
              {projectTask.length} Tareas
            </Badge>
          </div>
          <div id="botonera" className="flex flex-col justify-end items gap-3">
            <div className="flex flex-row gap-5 justify-end">
              <h3 className="flex gap-2 text-xl items-center">
                {getUsernameById(proyecto?.createdBy?.displayName)}
                <CircleUserRound />
              </h3>
            </div>
            <div className="flex gap-2 h-fit">
              <Button variant="outline" onClick={() => setPopup("Edit")}>
                <Pen />
                Editar
              </Button>
              <Button variant="outline" onClick={() => setPopup("Archive")}>
                <Archive />
                Archivar
              </Button>
              <Separator orientation="vertical" />

              <Button
                variant="outline"
                className="text-red-400 hover:text-red-400"
                onClick={() => setPopup("Delete")}
              >
                <Trash />
                Eliminar
              </Button>
            </div>
            <Button>
              <Plus />
              Crear tarea
            </Button>
          </div>
        </div>

        <div
          id="tabla-task"
          className="flex flex-col overflow-hidden rounded-md bg-background shadow-2xs p-3 gap-4 w-full"
        >
          <div>
            <div className="flex gap-2 items-center">
              <FilterSeletor
                items={item_f1}
                placeholder="Seleciona estados"
                value={status}
                onChange={setStatus}
              />
              <FilterSeletor
                items={item_f2}
                placeholder="Seleciona prioridad"
                value={priority}
                onChange={setPriority}
              />
              <FilterSeletor
                items={item_f3}
                placeholder="Seleciona miembros"
                value={member}
                onChange={setMember}
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
                placeholder="Buscar por nombre"
                onChange={(e) => setBusqueda(e.target.value)}
              />
            </div>
          </div>

          <DataTable
            columns={TaskColum}
            data={filterTask}
            onClic={(t) => {
              setSelectTask(t === selectTask ? null : t);
            }}
          />
        </div>
      </div>
    </div>
  );
}
