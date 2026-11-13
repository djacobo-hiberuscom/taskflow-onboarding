import { Button } from "@/components/ui/button";

interface PaginationProps {
  page: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  className?: string;
}

// Botonera Anterior / Siguiente
export function Pagination({
  page,
  totalPages,
  onPageChange,
  className,
}: PaginationProps) {
  const pages = Math.max(totalPages, 1);
  return (
    <div
      className={
        className ?? "relative z-10 flex items-center justify-end gap-2"
      }
    >
      <Button
        className="relative z-10"
        variant="outline"
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
      >
        Anterior
      </Button>
      <span className="text-sm tabular-nums">
        {page} / {pages}
      </span>
      <Button
        variant="outline"
        disabled={page >= pages}
        onClick={() => onPageChange(page + 1)}
      >
        Siguiente
      </Button>
    </div>
  );
}
