// Types
import type { Task } from "#lib/api/types";

// Parametros del compoenente
interface parameters {
  tarea?: Task;
}
// Compoenente
export function RowLastTareas({ tarea }: parameters) {
  return (
    <div className="p-5 border-y">
      <h1>{tarea?.title ?? "sin nombre"}</h1>
    </div>
  );
}