import { Button } from "@/components/ui/button";

interface PaginationProps {
  page: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  className?: string;
}

// Botonera Anterior / Siguiente
export function Pagination({ page, totalPages, onPageChange, className }: PaginationProps) {
  const pages = Math.max(totalPages, 1);
  return (
    <div className={className ?? "flex items-center justify-end gap-2"}>
      <Button variant="outline" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
        Anterior
      </Button>
      <span className="text-sm tabular-nums">
        {page} / {pages}
      </span>
      <Button variant="outline" disabled={page >= pages} onClick={() => onPageChange(page + 1)}>
        Siguiente
      </Button>
    </div>
  );
}
