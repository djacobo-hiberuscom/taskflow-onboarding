// COMPONENTES
import {
  Drawer,
  DrawerContent,
  Button,
  Separator,
  Badge,
  DrawerFooter,
} from "@/components/index.comp";
import { FilterSeletor } from "./index";
// Iconos
import { Folder } from "lucide-react";

// Pop up de delete confirm

// TYPOS
import type { Project, Task } from "@/lib/api/types";

import { getUsernameById, getUsernameByProjectId } from "@/tools/getUsername";
import { useState } from "react";
import { DeleteConfirmPopUp } from "@/components/common/popup/Delete-Confirm";
interface parameters {
  tarea?: Task | null;
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  proyecto?: Project;
}

export function DeailTaskDrawer({
  tarea,
  isOpen,
  onOpenChange,
  proyecto,
}: parameters) {

  const [confirmDeleteIsOpen, setConfirmDeleteIsOpen] = useState(false)

  const openDeleteConfirm = () => {
    setConfirmDeleteIsOpen(!confirmDeleteIsOpen)
  }

  return (
    <Drawer
      modal={false}
      disablePointerDismissal
      swipeDirection="right"
      open={isOpen}
      onOpenChange={onOpenChange}
    >
      <DeleteConfirmPopUp 
      modal={false}
      isOpen= {confirmDeleteIsOpen}
      onOpenChange={(open)=> {if (!open) setConfirmDeleteIsOpen(false)}}
      />
      <DrawerContent>
        <DetailTaskDrawerContainer tarea={tarea} proyecto={proyecto} />
        <DrawerFooter>
          <DrawerBotonera onDeleteClic={openDeleteConfirm}/>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  );
}

// FILTROS

interface containerParameters {
  tarea?: Task | null;
  proyecto?: Project;
}
function DetailTaskDrawerContainer({ tarea, proyecto }: containerParameters) {
  const [status, setStatus] = useState<string | null>(tarea?.status ?? null);
  const [member, setMember] = useState<string | null>(
    tarea?.assigned_id ?? null,
  );

  if (proyecto === undefined) return <h1>Error al cargar el proyecto</h1>;
  const item_f1 = [
    { label: "Hecho", value: "Done" },
    { label: "Sin hacer", value: "To Do" },
    { label: "En proceso", value: "In Progress" },
  ];
  const item_f2 = getUsernameByProjectId(proyecto.id).map((iid) => ({
    label: getUsernameById(iid) ?? "SinNombre",
    value: iid,
  }));

  return (
    <div className="p-2">
      <div className="border-b p-2 flex gap-2">
        <Folder />
        <h2 className="flex gap-1 items-center text-xs">{proyecto?.name}</h2>
      </div>

      <div className="flex flex-col flex-1 p-2 gap-4">
        <div className="flex flex-col">
          <h1 className="text-xl font-bold">{tarea?.title}</h1>
        </div>

        <div className="grid grid-cols-2 gap-2">
          <div className="min-w-0 flex flex-col gap-1">
            <span>Etado</span>
            <FilterSeletor
              className="w-full"
              items={item_f1}
              placeholder="Estado actual"
              value={status}
              onChange={setStatus}
            />
          </div>
          <div className="min-w-0 flex flex-col gap-1">
            <span>Asignado</span>
            <FilterSeletor
              className="w-full"
              items={item_f2}
              placeholder="Miembro asignado"
              value={member}
              onChange={setMember}
            />
          </div>
        </div>
        <Separator />
        <div>
          <h2 className="font-bold">Descripcion</h2>
          <p>{tarea?.description}</p>
        </div>
        <div id="Data">
          <div id="Prioridad" className="border-y p-5 flex">
            <h1 className="flex-1">Prioridad</h1>
            <Badge className={priorityClass(tarea?.priority)}>{tarea?.priority}</Badge>
          </div>
          <div id="Vencimiento" className="border-y p-5 flex">
            <h1 className="flex-1">Vencimiento</h1>
            <span>{tarea?.due_date}</span>
          </div>
          <div id="Prioridad" className="border-y p-5 flex">
            <h1 className="flex-1">Proyecto</h1>
            <span>{proyecto.name}</span>
          </div>
          <div id="Prioridad" className="border-y p-5 flex">
            <h1 className="flex-1">Creada</h1>
            <span>{tarea?.created_at}</span>
          </div>
          <div id="Prioridad" className="border-y p-5 flex">
            <h1 className="flex-1">Ultimo cambio</h1>
            <span>{tarea?.update_at}</span>
          </div>
        </div>
      </div>
      <div> </div>
    </div>
  );
}


interface par{
  onDeleteClic: () => void
}
function DrawerBotonera ({onDeleteClic}:par){
  return (
    <div className="flex justify-between">
      <Button variant="outline" className="text-white bg-[#4F46E5] hover:bg-[#4338CA] hover:text-white">Save</Button>
      <Button variant="outline" className="text-[#B91C1C]" onClick={onDeleteClic}>Eliminar</Button>
    </div>
  )
}

function priorityClass(prioridad?: Task["priority"]) {
  switch (prioridad) {
    case "High":
      return "bg-[#FEE2E2] text-[#991B1B]";
    case "Medium":
      return "bg-[#FEF3C7] text-[#92400E]";
    case "Low":
      return "bg-[#F1F5F9] text-[#334155]";
    default:
      return "";
  }
}
