// Resumen superior
export function HeaderTask({
  nTareas,
  nProyectos,
}: {
  nTareas: number;
  nProyectos: number;
}) {
  return (
    <div className="">
      <h1 className="text-2xl font-bold">Tareas</h1>
      <h1 className="text-xs text-muted-foreground">
        {nTareas} Tareas en {nProyectos} Proyectos
      </h1>
    </div>
  );
}
