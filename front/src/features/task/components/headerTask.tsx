// Resumen superior
export function HeaderTask({ nTareas, nProyectos }: { nTareas: number; nProyectos: number }) {
  return (
    <h1 className="text-xs text-muted-foreground">
      {nTareas} Tareas en {nProyectos} Proyectos
    </h1>
  );
}
