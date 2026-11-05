// Icono
import { Folder } from "lucide-react";

// Parametros Compoenente
interface parameters {
  name: string;
  number: number;
  descripcion?: string;
}

// Compoenente
export function StatCard({ name, number, descripcion }: parameters) {
  return (
    <div id="n-proyectos-activos" className="flex flex-col border rounded-2xl p-4 gap-10 min-w-2xs bg-background">
      <div className="flex justify-between gap-4">
        <h1 className="text-xl">{name}</h1>
        <Folder />
      </div>

      <h1 className="text-4xl font-bold">{number}</h1>
      <h1>{descripcion}</h1>
    </div>
  );
}