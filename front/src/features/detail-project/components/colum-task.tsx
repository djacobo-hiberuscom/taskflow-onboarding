import { createColumnHelper, type Column } from "@tanstack/react-table";
import { Button, TaskPriorityBadge, TaskStatusBadge } from "@/components/index.comp";
import { type DataTableFeatures } from "@/components/ui/data-table/data-table-features";
import type { Task } from "@/lib/api/types";
import { formatDateTime } from "@/lib/format";

const columnHelper = createColumnHelper<DataTableFeatures, Task>();

// Cabecera clicable que ordena la columna
function sortableHeader(label: string) {
  return function SortableHeader<TValue>({
    column,
  }: {
    column: Column<DataTableFeatures, Task, TValue>;
  }) {
    return (
      <Button variant="ghost" onClick={column.getToggleSortingHandler()} className="-ml-2.5">
        {label}
        {column.getIsSorted() === "asc" && " ↑"}
        {column.getIsSorted() === "desc" && " ↓"}
      </Button>
    );
  };
}

// Columnas de la tabla de tareas del proyecto
export const TaskColum = columnHelper.columns([
  columnHelper.accessor("title", { header: sortableHeader("Nombre") }),
  columnHelper.accessor((t) => t.assignee?.displayName ?? "Sin asignar", {
    id: "assignee",
    header: sortableHeader("Usuario asignado"),
  }),
  columnHelper.accessor("priority", {
    header: sortableHeader("Prioridad"),
    cell: (info) => <TaskPriorityBadge priority={info.getValue()} />,
  }),
  columnHelper.accessor("status", {
    header: sortableHeader("Estado"),
    cell: (info) => <TaskStatusBadge status={info.getValue()} />,
  }),
  columnHelper.accessor("updatedAt", {
    header: sortableHeader("Último cambio"),
    cell: (info) => formatDateTime(info.getValue()),
  }),
]);
