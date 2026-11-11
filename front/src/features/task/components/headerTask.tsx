// Resumen superior
export function HeaderTask({ nTareas, nProyectos }: { nTareas: number; nProyectos: number }) {
  return (
    <h1 className="text-xs text-gray-500">
      {nTareas} Tareas en {nProyectos} Proyectos
    </h1>
  );
}
