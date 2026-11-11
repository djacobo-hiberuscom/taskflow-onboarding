import { RowLastTareas } from "./TaskRow";

// Últimas tareas (TODO: conectar con la API)
export function LastChanges() {
  return (
    <div className="flex flex-col gap-3 border rounded-xl overflow-hidden p-4 bg-background">
      <h1>Últimas tareas</h1>
      <div className="flex flex-col flex-1">
        {Array.from({ length: 8 }, (_, i) => (
          <RowLastTareas key={i} />
        ))}
      </div>
    </div>
  );
}
