import { ChevronLeft, ChevronRight } from "lucide-react";
import { Button } from "@/components/index.comp";

interface ProjectsPaginationProps {
  page: number;
  totalPages: number;
  total: number;
  onChange: (p: number) => void;
}

// "Página X de Y · N proyectos" + Anterior / números / Siguiente
export function ProjectsPagination({ page, totalPages, total, onChange }: ProjectsPaginationProps) {
  return (
    <footer className="flex flex-wrap items-center justify-between gap-2 text-sm text-muted-foreground">
      <span>
        Página {page} de {totalPages} · {total} proyecto{total === 1 ? "" : "s"}
      </span>

      <nav className="flex items-center gap-1" aria-label="Paginación">
        <Button variant="outline" size="sm" disabled={page === 1} onClick={() => onChange(page - 1)}>
          <ChevronLeft />
          Anterior
        </Button>

        {Array.from({ length: totalPages }, (_, i) => i + 1).map((n) => (
          <Button
            key={n}
            size="sm"
            variant={n === page ? "secondary" : "ghost"}
            aria-current={n === page ? "page" : undefined}
            onClick={() => onChange(n)}
          >
            {n}
          </Button>
        ))}

        <Button
          variant="outline"
          size="sm"
          disabled={page === totalPages}
          onClick={() => onChange(page + 1)}
        >
          Siguiente
          <ChevronRight />
        </Button>
      </nav>
    </footer>
  );
}
