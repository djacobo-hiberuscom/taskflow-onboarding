import { createColumnHelper } from "@tanstack/react-table"

import { type DataTableFeatures } from "@/components/ui/data-table/data-table-features"
import type { Task } from "@/lib/api/types"

const columnHelper = createColumnHelper<DataTableFeatures, Task>()



export const TaskColum = columnHelper.columns([
  columnHelper.accessor("title", {
    header: "Title",
  }),
  columnHelper.accessor("description", {
    header: "Description",
    cell: (info) => (
      <span className="block max-w-[300px] truncate">{info.getValue()}</span>
    ),
  }),
  columnHelper.accessor("priority", {
    header: "Priority",
  }),
  columnHelper.accessor("status", {
    header: "Status"
  }),
  columnHelper.accessor("update_at", {
    header: "LastChange"
  })
])