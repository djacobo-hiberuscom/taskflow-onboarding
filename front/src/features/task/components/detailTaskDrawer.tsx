// COMPONENTES
import {
  Drawer,
  DrawerContent,
  Button,
  Separator,
  Badge,
  DrawerFooter,
  toast,
} from "@/components/index.comp";
import { FilterSeletor } from "./index.comp";
// Iconos
import { Folder } from "lucide-react";

// TYPOS
import type { Project, Task, TaskStatus } from "@/lib/api/types";

import { getUsernameById, getUsernameByProjectId } from "@/tools/getUsername";
import { useState } from "react";
import { DeleteConfirmPopUp } from "@/components/common/popup/Delete-Confirm";
import {
  useChangeTaskAssignee,
  useChangeTaskStatus,
  useDeleteTask,
} from "@/hooks/use-task";
import { useProject } from "@/hooks/use-project";
import { ApiError } from "@/lib/api/client";
import { NotFound } from "@/app/layouts";


interface parameters {
  isAdmin?: boolean;
  tarea?: Task | null;
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  proyecto?: string;
}

export function DeailTaskDrawer({
  tarea,
  isOpen,
  onOpenChange,
  proyecto,
  isAdmin,
}: parameters) {
  const [confirmDeleteIsOpen, setConfirmDeleteIsOpen] = useState(false);
  const deleteTask = useDeleteTask();
  const {
    data: proyectoDetail,
    isLoading: proyectoLoading,
    error: proyectoError,
  } = useProject(proyecto);

  if (proyectoLoading) return <h1>Cargando</h1>;
  if (proyectoError instanceof ApiError && proyectoError.status === 404)
    return <NotFound />;
  if (proyectoError) return <h1>{proyectoError.message}</h1>;

  const openDeleteConfirm = () => {
    setConfirmDeleteIsOpen(!confirmDeleteIsOpen);
  };

  const handleDeleteClic = (id: string) => {
    deleteTask.mutate(id, {
      onSuccess: () => {
        setConfirmDeleteIsOpen(false)
        onOpenChange(false);
        openToast("Se ha borrado correctamente la tarea", "success");
      },
      onError: () => {
        setConfirmDeleteIsOpen(false)
        openToast("Error al borrar la tarea", "error");
      },
    });
  };

  return (
    <Drawer
      modal={false}
      disablePointerDismissal
      swipeDirection="right"
      open={isOpen}
      onOpenChange={onOpenChange}
    >
      <DeleteConfirmPopUp
        onClic={() => {
          if (tarea) handleDeleteClic(tarea.id);
        }}
        modal={false}
        isOpen={confirmDeleteIsOpen}
        onOpenChange={(open) => {
          if (!open) setConfirmDeleteIsOpen(false);
        }}
      />
      <DrawerContent>
        <DetailTaskDrawerContainer
          tarea={tarea}
          proyecto={proyectoDetail}
          isAdmin={isAdmin}
        />
        <DrawerFooter>
          <DrawerBotonera onDeleteClic={openDeleteConfirm} />
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  );
}

// FILTROS

interface containerParameters {
  tarea?: Task | null;
  proyecto?: Project;
  isAdmin?: boolean;
}
function DetailTaskDrawerContainer({
  tarea,
  proyecto,
  isAdmin,
}: containerParameters) {
  const changeStatus = useChangeTaskStatus();
  const changeAssegnee = useChangeTaskAssignee();
  //const [status, setStatus] = useState<string | null>(tarea?.status ?? null);
  const [member, setMember] = useState<string | null>(
    tarea?.assignee?.displayName ?? null,
  );

  if (proyecto === undefined) return <h1>Error al cargar el proyecto</h1>;

  // Filtros
  const item_f1 = [
    { label: "Hecho", value: "Done" },
    { label: "Sin hacer", value: "ToDo" },
    { label: "En proceso", value: "InProgress" },
  ];
  const item_f2 = getUsernameByProjectId(proyecto.id).map((iid) => ({
    label: getUsernameById(iid) ?? "SinNombre",
    value: iid,
  }));

  // Componente
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
              value={tarea?.status ?? null}
              disabled={changeStatus.isPending}
              onChange={(v) => {
                if (!tarea || !v || v === tarea.status) return;
                changeStatus.mutate({ id: tarea.id, status: v as TaskStatus });
              }}
            />
          </div>
          <div className="min-w-0 flex flex-col gap-1">
            <span>Asignado</span>
            <FilterSeletor
              disabled={!isAdmin}
              className="w-full"
              items={item_f2}
              placeholder="Miembro asignado"
              value={member}
              onChange={(v) => {
                if (v && v !== tarea?.assignee?.id)
                  changeAssegnee.mutate({ id: tarea?.id ?? "", assigneeId: v });
                setMember(v);
              }}
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
            <Badge className={priorityClass(tarea?.priority)}>
              {tarea?.priority}
            </Badge>
          </div>
          <div id="Vencimiento" className="border-y p-5 flex">
            <h1 className="flex-1">Vencimiento</h1>
            <span>{tarea?.dueDate}</span>
          </div>
          <div id="Prioridad" className="border-y p-5 flex">
            <h1 className="flex-1">Proyecto</h1>
            <span>{proyecto.name}</span>
          </div>
          <div id="Prioridad" className="border-y p-5 flex">
            <h1 className="flex-1">Creada</h1>
            <span>{tarea?.createdAt}</span>
          </div>
          <div id="Prioridad" className="border-y p-5 flex">
            <h1 className="flex-1">Ultimo cambio</h1>
            <span>{tarea?.updatedAt}</span>
          </div>
        </div>
      </div>
    </div>
  );
}

interface par {
  onDeleteClic: () => void;
}
function DrawerBotonera({ onDeleteClic }: par) {
  return (
    <div className="flex justify-end">
      <Button variant="destructive" onClick={onDeleteClic}>
        Eliminar
      </Button>
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
      return "bg-[#F1F5F9] text-[#334155]";
    default:
      return "";
  }
}

// Abrir un toast
const openToast = (message: string, type: string) => {
  toast.add({
    type: type,
    description: message,
  });
};
