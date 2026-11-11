import type { Task } from "@/lib/api/types";

// Fila de "últimas tareas"
export function RowLastTareas({ tarea }: { tarea?: Task }) {
  return (
    <div className="p-5 border-y">
      <h1>{tarea?.title ?? "sin nombre"}</h1>
    </div>
  );
}
