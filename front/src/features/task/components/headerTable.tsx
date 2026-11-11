import { cn } from "cn";
import { TASK_GRID } from "./types";

// Cabecera de la tabla
export function HeaderTableTask() {
  return (
    <div className={cn(TASK_GRID, "w-full text-subtle-foreground border rounded-t-md bg-card p-5")}>
      <span>Nombre</span>
      <span>Proyecto</span>
      <span>Estado</span>
      <span>Prioridad</span>
      <span>Asignado</span>
      <span>Vencimiento</span>
    </div>
  );
}
