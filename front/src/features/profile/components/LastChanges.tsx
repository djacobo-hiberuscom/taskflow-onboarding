import { RowLastTareas } from "./index";

// Parametros

// COMPONENTE
export function LastChanges() {
  return (
    <div
      id="LasChanges"
      className="flex flex-col gap-3 border rounded-xl overflow-hidden p-4 bg-background"
    >
      <h1>Ultimas tareas</h1>
      <div className="flex flex-col flex-1">
        <RowLastTareas />
        <RowLastTareas />
        <RowLastTareas />
        <RowLastTareas />
        <RowLastTareas />
        <RowLastTareas />
        <RowLastTareas />
        <RowLastTareas />
      </div>
    </div>
  );
}
