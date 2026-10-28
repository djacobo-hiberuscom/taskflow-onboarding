import { createColumnHelper } from "@tanstack/react-table"
import { Button } from "#components/ui/button"
import { type DataTableFeatures } from "@/components/ui/data-table/data-table-features"
import type { Task } from "@/lib/api/types"
import { getUsernameById } from "@/tools/getUsername"

const columnHelper = createColumnHelper<DataTableFeatures, Task>()



export const TaskColum = columnHelper.columns([
  columnHelper.accessor("title", {
    header: ({
      column
    }) => (
      <Button variant="ghost" onClick={column.getToggleSortingHandler()} className="-ml-2.5">
        Nombre
        {column.getIsSorted() === "asc" &&  " ↑"}
        {column.getIsSorted() === "desc" && " ↓"}
      </Button>
    ),
  }),
  columnHelper.accessor("description", {
    header: ({
      column
    }) => (
      <Button variant="ghost" onClick={column.getToggleSortingHandler()} className="-ml-2.5">
        Descripcion
        {column.getIsSorted() === "asc" &&  " ↑"}
        {column.getIsSorted() === "desc" && " ↓"}
      </Button>
    ),
    cell: (info) => (
      <span className="block max-w-[300px] truncate">{info.getValue()}</span>
    ),
  }),
  columnHelper.accessor("assigned_id", {
    header :({
      column
    }) => (
      <Button variant="ghost" onClick={column.getToggleSortingHandler()} className="-ml-2.5">
        Usuario Asignado
        {column.getIsSorted() === "asc" &&  " ↑"}
        {column.getIsSorted() === "desc" && " ↓"}
      </Button>
    ),
    cell: (info) => (
      <span>{getUsernameById(info.getValue())}</span>
    )
  })
  ,
  columnHelper.accessor("priority", {
    header: ({
      column
    }) => (
      <Button variant="ghost" onClick={column.getToggleSortingHandler()} className="-ml-2.5">
        Prioridad
        {column.getIsSorted() === "asc" &&  " ↑"}
        {column.getIsSorted() === "desc" && " ↓"}
      </Button>
    ),
  }),
  columnHelper.accessor("status", {
    header: ({
      column
    }) => (
      <Button variant="ghost" onClick={column.getToggleSortingHandler()} className="-ml-2.5">
        Estado
        {column.getIsSorted() === "asc" &&  " ↑"}
        {column.getIsSorted() === "desc" && " ↓"}
      </Button>
    )
  }),
  columnHelper.accessor("update_at", {
    header: ({
      column
    }) => (
      <Button variant="ghost" onClick={column.getToggleSortingHandler()} className="-ml-2.5">
        Ultimo cambio
        {column.getIsSorted() === "asc" &&  " ↑"}
        {column.getIsSorted() === "desc" && " ↓"}
      </Button>
    )
  })
])