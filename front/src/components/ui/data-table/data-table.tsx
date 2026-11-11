import { useTable, type ColumnDef, type RowData } from "@tanstack/react-table";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { Button } from "../button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "../../../components/ui/table";
import { features, type DataTableFeatures } from "./data-table-features";


interface DataTableProps<TData extends RowData> {
  columns: ColumnDef<DataTableFeatures, TData>[];
  data: TData[];
  onClic?: (row: TData) => void
  // Texto del pie: "Mostrando 1–8 de 8 {itemLabel}"
  itemLabel?: string
}

export function DataTable<TData extends RowData>({
  columns,
  data,
  onClic,
  itemLabel = "resultados",
}: DataTableProps<TData>) {
  const table = useTable({
    features,
    data,
    columns,
  });

  const { pageIndex, pageSize } = table.state.pagination;
  const total = data.length;
  const desde = total === 0 ? 0 : pageIndex * pageSize + 1;
  const hasta = Math.min((pageIndex + 1) * pageSize, total);

  return (
    <div className="overflow-hidden rounded-xl border bg-card">
      <Table>
        <TableHeader>
          {table.getHeaderGroups().map((headerGroup) => (
            <TableRow key={headerGroup.id} className="hover:bg-transparent">
              {headerGroup.headers.map((header) => {
                return (
                  <TableHead key={header.id} className="h-11 px-4 text-xs font-medium text-muted-foreground">
                    {header.isPlaceholder ? null : (
                      <table.FlexRender header={header} />
                    )}
                  </TableHead>
                );
              })}
            </TableRow>
          ))}
        </TableHeader>
        <TableBody>
          {table.getRowModel().rows?.length ? (
            table.getRowModel().rows.map((row) => (
              <TableRow
                key={row.id}
                data-state={row.getIsSelected() && "selected"}
                onClick={() => onClic?.(row.original)}
                className={onClic ? "cursor-pointer" : undefined}
              >
                {row.getVisibleCells().map((cell) => (
                  <TableCell key={cell.id} className="h-14 px-4">
                    <table.FlexRender cell={cell} />
                  </TableCell>
                ))}
              </TableRow>
            ))
          ) : (
            <TableRow>
              <TableCell colSpan={columns.length} className="h-24 text-center">
                Sin resultados
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
      <div className="flex flex-wrap items-center justify-between gap-2 border-t px-4 py-3">
        <span className="text-xs text-muted-foreground">
          Mostrando {desde}–{hasta} de {total} {itemLabel}
        </span>

        <div className="flex items-center gap-1">
          <Button
            variant="outline"
            size="sm"
            onClick={() => table.previousPage()}
            disabled={!table.getCanPreviousPage()}
          >
            <ChevronLeft />
            Anterior
          </Button>
          <span className="flex size-8 items-center justify-center rounded-md border text-xs font-medium text-foreground tabular-nums">
            {pageIndex + 1}
          </span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => table.nextPage()}
            disabled={!table.getCanNextPage()}
          >
            Siguiente
            <ChevronRight />
          </Button>
        </div>
      </div>
    </div>
  );
}
