// Parametros del compoenente
interface params {
    Ntareas: number,
    Nproyectos: number
}

// header
export function HeaderTask({Ntareas, Nproyectos}: params) {
  return (
    <div id="Header" className="">
      <h1 className="text-xs text-gray-500">
        {Ntareas} Tareas en {Nproyectos} Proyectos
      </h1>
    </div>
  );
}