import { createColumnHelper } from "@tanstack/react-table";

import { type DataTableFeatures } from "@/components/ui/data-table/data-table-features";
import type { Project } from "@/lib/api/types";
import { Button } from "#components/ui/button";

const columnHelper = createColumnHelper<DataTableFeatures, Project>();

export const ProjectColum = columnHelper.columns([
  columnHelper.accessor("name", {
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
      <button className="block max-w-[300px] truncate" onClick={() => console.log("clic")}>{info.getValue()}</button>
    ),
  }),
  columnHelper.accessor("created_at", {
    header: ({
      column
    }) => (
      <Button variant="ghost" onClick={column.getToggleSortingHandler()} className="-ml-2.5">
        Fecha de creacion 
        {column.getIsSorted() === "asc" &&  " ↑"}
        {column.getIsSorted() === "desc" && " ↓"}
      </Button>
    ),
  }),
  columnHelper.accessor("updated_at", {
    header: ({
      column
    }) => (
      <Button variant="ghost" onClick={column.getToggleSortingHandler()} className="-ml-2.5">
        Ultimo cambio
        {column.getIsSorted() === "asc" &&  " ↑"}
        {column.getIsSorted() === "desc" && " ↓"}
      </Button>
    ),
  }),
]);
