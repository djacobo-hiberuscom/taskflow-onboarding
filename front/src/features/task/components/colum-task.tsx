import { createColumnHelper } from "@tanstack/react-table";
import { Button } from "#components/ui/button";
import { type DataTableFeatures } from "@/components/ui/data-table/data-table-features";
import type { Task } from "@/lib/api/types";

const columnHelper = createColumnHelper<DataTableFeatures, Task>();

export const TaskColum = columnHelper.columns([
  columnHelper.accessor("title", {
    header: ({ column }) => (
      <Button
        variant="ghost"
        onClick={column.getToggleSortingHandler()}
        className="-ml-2.5"
      >
        Nombre
        {column.getIsSorted() === "asc" && " ↑"}
        {column.getIsSorted() === "desc" && " ↓"}
      </Button>
    ),
  }),
  columnHelper.accessor("assignee", {
    header: ({ column }) => (
      <Button
        variant="ghost"
        onClick={column.getToggleSortingHandler()}
        className="-ml-2.5"
      >
        Usuario Asignado
        {column.getIsSorted() === "asc" && " ↑"}
        {column.getIsSorted() === "desc" && " ↓"}
      </Button>
    ),
    cell: (info) => {
      const assignee = info.getValue();
      return <span>{assignee?.displayName ?? "Sin asignar"}</span>;
    },
  }),
  columnHelper.accessor("priority", {
    header: ({ column }) => (
      <Button
        variant="ghost"
        onClick={column.getToggleSortingHandler()}
        className="-ml-2.5"
      >
        Prioridad
        {column.getIsSorted() === "asc" && " ↑"}
        {column.getIsSorted() === "desc" && " ↓"}
      </Button>
    ),
  }),
  columnHelper.accessor("status", {
    header: ({ column }) => (
      <Button
        variant="ghost"
        onClick={column.getToggleSortingHandler()}
        className="-ml-2.5"
      >
        Estado
        {column.getIsSorted() === "asc" && " ↑"}
        {column.getIsSorted() === "desc" && " ↓"}
      </Button>
    ),
  }),
  columnHelper.accessor("updatedAt", {
    header: ({ column }) => (
      <Button
        variant="ghost"
        onClick={column.getToggleSortingHandler()}
        className="-ml-2.5"
      >
        Ultimo cambio
        {column.getIsSorted() === "asc" && " ↑"}
        {column.getIsSorted() === "desc" && " ↓"}
      </Button>
    ),
  }),
]);
