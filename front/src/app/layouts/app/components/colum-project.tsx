import { createColumnHelper } from "@tanstack/react-table"

import { type DataTableFeatures } from "@/components/ui/data-table/data-table-features"
import type { Project } from "@/lib/api/types"

const columnHelper = createColumnHelper<DataTableFeatures, Project>()

export const ProjectColum = columnHelper.columns([
  columnHelper.accessor("name", {
    header: "Name",
  }),
  columnHelper.accessor("description", {
    header: "Description",
  }),
  columnHelper.accessor("created_at", {
    header: "CreatedAt",
  }),
  columnHelper.accessor("updated_at", {
    header: "LastChange"
  })
])