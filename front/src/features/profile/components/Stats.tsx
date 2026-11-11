// Estadísticas del usuario (TODO: conectar con la API)
export function StatsMe() {
  const rows = ["Tareas completadas", "Tareas pendientes", "Proyectos"];
  return (
    <div className="bg-background p-5 flex flex-col gap-3 border rounded-xl">
      <h1>Mis estadísticas / Actividad</h1>
      <div>
        {rows.map((label) => (
          <div key={label} className="flex gap-2">
            <h2 className="flex flex-1">{label}</h2>
            <span>N</span>
          </div>
        ))}
      </div>
    </div>
  );
}
