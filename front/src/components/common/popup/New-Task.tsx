// hooks
import { useCreateTask } from "#hooks/use-task";
import { useState } from "react";
import {
  DialogContent,
  Dialog,
  toast,
  Input,
  Button,
  FilterSeletor,
  Popover,
  PopoverContent,
  Field,
  FieldLabel,
  Calendar,
  PopoverTrigger,
} from "@/components/index.comp";
import { format } from "date-fns";
// icon
import { X } from "lucide-react";
import type { TaskStatus, TaskPriority, Project } from "@/lib/api/types";

// Toast
const openSuccessToast = () => {
  toast.add({
    type: "success",
    description: "Tarea creada correctamente",
  });
};
const openErrorToast = () => {
  toast.add({
    type: "error",
    description: "Error al crear la tarea",
  });
};

// parametros del componente
interface params {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  onClose: () => void;
  proyecto: Project;
}
//popup
export function NewTaskPopup({
  isOpen,
  onOpenChange,
  onClose,
  proyecto,
}: params) {
  // hooks
  const useCreateNewTask = useCreateTask();

  // states
  const [nombre, setNombre] = useState("");
  const [descripcion, setDescripcion] = useState("");
  const [miembro, setMiembro] = useState<string | null>(null);
  const [estatus, setEstatus] = useState<TaskStatus | null>(null);
  const [prioridad, setPrioridad] = useState<TaskPriority | null>(null);
  const [date, setDate] = useState<Date | undefined>(undefined);

  // FILTROS
  const ESTADO = [
    { label: "Completado", value: "Done" },
    { label: "Pendiente", value: "ToDo" },
    { label: "En curso", value: "InProgress" },
  ];
  const PRIORIDAD = [
    { label: "Alta", value: "High" },
    { label: "Madia", value: "Medium" },
    { label: "Baja", value: "Low" },
  ];
  const MEMBERS = proyecto.members.map((u) => {
    return { label: u.displayName, value: u.id };
  });
  // quitar filtros
  const quitarFilters = () => {
    setEstatus(null);
    setPrioridad(null);
    setMiembro(null);
    setDate(undefined);
  };
  const handleSaveClic = () => {
    if (!nombre.trim() || !prioridad) return;
    useCreateNewTask.mutate(
      {
        title: nombre.trim(),
        projectId: proyecto.id,
        description: descripcion.trim() || undefined,
        priority: prioridad || undefined,
        status: estatus || undefined,
        dueDate: date ? format(date, "yyyy-MM-dd") : null,
        assigneeId: miembro,
      },
      {
        onSuccess: () => {
          setNombre("");
          setDescripcion("");
          quitarFilters();
          onClose();
          openSuccessToast();
        },
        onError: (error) => {
          setNombre("");
          setDescripcion("");
          quitarFilters();
          onClose();
          openErrorToast();
          console.log("Error al crear tarea ", error)
        },
      },
    );
  };
  return (
    <div>
      <Dialog open={isOpen} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-3xl h-2xl ">
          <div className="flex flex-col gap-2">
            <h1 className="text-xl">Crear una nueva tarea</h1>
            <span>Nombre</span>
            <Input
              placeholder="Nombre"
              onChange={(e) => setNombre(e.target.value)}
              value={nombre}
            />
            <span>Descripcion*</span>
            <Input
              placeholder="Descripcion"
              onChange={(e) => setDescripcion(e.target.value)}
              value={descripcion}
            />
            <div className="flex justify-center items-center gap-2">
              <div>
                <span>Miembro</span>
                <FilterSeletor
                  items={MEMBERS}
                  placeholder="Asignar"
                  value={miembro}
                  onChange={(e) => setMiembro(e)}
                />
              </div>
              <div>
                <span>Estatus</span>
                <FilterSeletor
                  items={ESTADO}
                  placeholder="Estado"
                  value={estatus}
                  onChange={(e) => setEstatus(e as TaskStatus)}
                />
              </div>
              <div>
                <span>Prioridad</span>
                <FilterSeletor
                  items={PRIORIDAD}
                  placeholder="Prioridad"
                  value={prioridad}
                  onChange={(e) => setPrioridad(e as TaskPriority)}
                />
              </div>
              <div>
                <Field className="mx-auto w-44">
                  <FieldLabel htmlFor="date-picker-simple">Date</FieldLabel>
                  <Popover>
                    <PopoverTrigger
                      render={
                        <Button
                          variant="outline"
                          id="date-picker-simple"
                          className="justify-start font-normal"
                        >
                          {date ? (
                            format(date, "PPP")
                          ) : (
                            <span>Pick a date</span>
                          )}
                        </Button>
                      }
                    />
                    <PopoverContent className="w-auto p-0" align="start">
                      <Calendar
                        mode="single"
                        selected={date ?? undefined}
                        onSelect={setDate}
                        defaultMonth={date ?? undefined}
                      />
                    </PopoverContent>
                  </Popover>
                </Field>
              </div>
            </div>
            <div className="flex gap-2 justify-end">
              <Button
                variant="outline"
                className="text-red-500 hover:brightness-98 hover:text-red-5000"
                onClick={quitarFilters}
              >
                <X />
              </Button>
              <Button
                variant="outline"
                className="text-red-500 hover:brightness-98 hover:text-red-500"
                onClick={onClose}
              >
                Cancelar
              </Button>
              <Button onClick={handleSaveClic}>Guardar</Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
