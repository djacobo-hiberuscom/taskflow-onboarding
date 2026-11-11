import type { LucideIcon } from "lucide-react";

interface StatCardProps {
  name: string;
  number: number;
  icon: LucideIcon;
  descripcion?: string;
}

// Tarjeta con un número destacado
export function StatCard({ name, number, icon: Icon, descripcion }: StatCardProps) {
  return (
    <div className="flex flex-col border rounded-2xl p-4 gap-10 min-w-2xs bg-background">
      <div className="flex justify-between gap-4">
        <h2 className="text-xl">{name}</h2>
        <Icon />
      </div>
      <p className="text-4xl font-bold">{number}</p>
      {descripcion && <p>{descripcion}</p>}
    </div>
  );
}
