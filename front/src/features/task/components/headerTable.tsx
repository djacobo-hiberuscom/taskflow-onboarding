// Header Table
export function HeaderTableTask() {
  return (
    <div
      id="HeaderTable"
      className="grid grid-cols-6 items-center w-full text-gray-400 border rounded-t-2xl p-5"
    >
      <span>Nombre</span>
      <span>Proyecto</span>
      <span>Estado</span>
      <span>Prioridad</span>
      <span>Asignado</span>
      <span>vencimiento</span>
    </div>
  );
}