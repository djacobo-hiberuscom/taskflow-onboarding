import { createColumnHelper, type Column } from "@tanstack/react-table";
import { Button, TaskPriorityBadge, TaskStatusBadge } from "@/components";
import { type DataTableFeatures } from "@/components/ui/data-table/data-table-features";
import type { Task } from "@/lib/api/types";
import { formatDateTime, formatRelative } from "@/lib/format";
import { UserCell } from "./MemberAvatars";

const columnHelper = createColumnHelper<DataTableFeatures, Task>();

// Cabecera clicable que ordena la columna
function sortableHeader(label: string) {
  return function SortableHeader<TValue>({
    column,
  }: {
    column: Column<DataTableFeatures, Task, TValue>;
  }) {
    return (
      <Button
        variant="ghost"
        size="sm"
        onClick={column.getToggleSortingHandler()}
        className="-ml-2.5 h-7 text-xs font-medium text-muted-foreground"
      >
        {label}
        {column.getIsSorted() === "asc" && " ↑"}
        {column.getIsSorted() === "desc" && " ↓"}
      </Button>
    );
  };
}

// Columnas de la tabla de tareas del proyecto
export const TaskColum = columnHelper.columns([
  columnHelper.accessor("title", {
    header: sortableHeader("Nombre"),
    cell: (info) => <span className="font-medium">{info.getValue()}</span>,
  }),
  columnHelper.accessor((t) => t.assignee?.displayName ?? "", {
    id: "assignee",
    header: sortableHeader("Asignado"),
    cell: (info) => <UserCell user={info.row.original.assignee} />,
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
    cell: (info) => (
      <span className="text-muted-foreground" title={formatDateTime(info.getValue())}>
        {formatRelative(info.getValue())}
      </span>
    ),
  }),
]);
