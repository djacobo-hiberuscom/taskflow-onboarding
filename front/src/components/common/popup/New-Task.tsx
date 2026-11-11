import { useState } from "react";
import { format } from "date-fns";
import { X } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Dialog, DialogContent } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { DatePicker } from "@/components/common/DatePicker";
import { FilterSelector } from "@/components/common/FilterSelector";
import { useCreateTask } from "@/hooks/use-task";
import type { ProjectDetail, TaskPriority, TaskStatus } from "@/lib/api/types";
import { TASK_PRIORITY_OPTIONS, TASK_STATUS_OPTIONS } from "@/lib/constants/task";
import { notify } from "@/lib/notify";

interface NewTaskPopupProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  proyecto: ProjectDetail;
}

// Crear una tarea dentro de un proyecto
export function NewTaskPopup({ isOpen, onOpenChange, proyecto }: NewTaskPopupProps) {
  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-3xl">
        <NewTaskForm proyecto={proyecto} onClose={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  );
}

// El formulario se monta al abrir el diálogo -> el estado se resetea solo
function NewTaskForm({ proyecto, onClose }: { proyecto: ProjectDetail; onClose: () => void }) {
  const createTask = useCreateTask();

  const [nombre, setNombre] = useState("");
  const [descripcion, setDescripcion] = useState("");
  const [miembro, setMiembro] = useState<string | null>(null);
  const [estado, setEstado] = useState<TaskStatus | null>(null);
  const [prioridad, setPrioridad] = useState<TaskPriority | null>(null);
  const [fecha, setFecha] = useState<Date | undefined>(undefined);

  const memberOptions = proyecto.members.map((u) => ({ label: u.displayName, value: u.id }));
  const canSave = nombre.trim().length > 0 && prioridad !== null && !createTask.isPending;

  const limpiar = () => {
    setNombre("");
    setDescripcion("");
    setMiembro(null);
    setEstado(null);
    setPrioridad(null);
    setFecha(undefined);
  };

  const handleSave = () => {
    if (!canSave || !prioridad) return;
    createTask.mutate(
      {
        projectId: proyecto.id,
        title: nombre.trim(),
        description: descripcion.trim() || undefined,
        priority: prioridad,
        status: estado ?? undefined,
        dueDate: fecha ? format(fecha, "yyyy-MM-dd") : null,
        assigneeId: miembro,
      },
      {
        onSuccess: () => {
          onClose();
          notify.success("Tarea creada correctamente");
        },
        onError: () => notify.error("Error al crear la tarea"),
      },
    );
  };

  return (
    <div className="flex flex-col gap-2">
      <h1 className="text-xl">Crear una nueva tarea</h1>

      <span>Nombre*</span>
      <Input placeholder="Nombre" value={nombre} onChange={(e) => setNombre(e.target.value)} />

      <span>Descripción</span>
      <Input
        placeholder="Descripción"
        value={descripcion}
        onChange={(e) => setDescripcion(e.target.value)}
      />

      <div className="flex justify-center items-end gap-2">
        <div>
          <span>Miembro</span>
          <FilterSelector
            items={memberOptions}
            placeholder="Asignar"
            value={miembro}
            onChange={setMiembro}
          />
        </div>
        <div>
          <span>Estado</span>
          <FilterSelector
            items={TASK_STATUS_OPTIONS}
            placeholder="Estado"
            value={estado}
            onChange={setEstado}
          />
        </div>
        <div>
          <span>Prioridad*</span>
          <FilterSelector
            items={TASK_PRIORITY_OPTIONS}
            placeholder="Prioridad"
            value={prioridad}
            onChange={setPrioridad}
          />
        </div>
        <DatePicker id="new-task-due-date" label="Vencimiento" value={fecha} onChange={setFecha} />
      </div>

      <div className="flex gap-2 justify-end">
        <Button variant="outline" className="text-red-500 hover:text-red-500" onClick={limpiar}>
          <X />
        </Button>
        <Button variant="outline" className="text-red-500 hover:text-red-500" onClick={onClose}>
          Cancelar
        </Button>
        <Button disabled={!canSave} onClick={handleSave}>
          {createTask.isPending ? "Guardando..." : "Guardar"}
        </Button>
      </div>
    </div>
  );
}
