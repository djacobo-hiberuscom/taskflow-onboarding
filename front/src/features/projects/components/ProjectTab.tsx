import { Star } from "lucide-react";

interface ProjectsTabsProps {
  total: number;
}

// Pestañas. "Favoritos" es solo visual hasta que el back lo soporte
export function ProjectsTabs({ total }: ProjectsTabsProps) {
  return (
    <div role="tablist" className="flex gap-1 rounded-lg bg-muted p-1">
      <button
        role="tab"
        aria-selected
        className="flex items-center gap-1.5 rounded-md bg-background px-3 py-1 text-sm font-medium shadow-sm"
      >
        Todos
        <span className="text-xs text-muted-foreground">{total}</span>
      </button>

      <button
        role="tab"
        aria-selected={false}
        disabled
        title="Próximamente"
        className="flex cursor-not-allowed items-center gap-1.5 rounded-md px-3 py-1 text-sm font-medium text-muted-foreground"
      >
        <Star className="size-3.5" />
        Favoritos
      </button>
    </div>
  );
}
