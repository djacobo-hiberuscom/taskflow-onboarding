// Types
import type { Task } from "#lib/api/types";
// compoenentes
import { Badge } from "@/components/index.comp";


// TableRow
interface rowParams {
  tarea: Task;
  onClictask: () => void;
}
export function RowTableTask({ tarea, onClictask }: rowParams) {
  return (
    <div
      className="grid grid-cols-6 justify-between w-full bg-background p-4 border-b gap-3 hover:brightness-95"
      onClick={onClictask}
    >
      <span className="truncate">{tarea.title}</span>
      <span className="truncate">{tarea.project_id}</span>
      <Badge className={statusClass(tarea.status)}>{tarea.status}</Badge>
      <Badge className={priorityClass(tarea.priority)}>{tarea.priority}</Badge>
      <span className="truncate">{tarea.assigned_id}</span>
      <span>{tarea.due_date}</span>
    </div>
  );
}

function priorityClass(prioridad?: Task["priority"]) {
  switch (prioridad) {
    case "High":
      return "bg-[#FEE2E2] text-[#991B1B]";
    case "Medium":
      return "bg-[#FEF3C7] text-[#92400E]";
    case "Low":
      return "bg-[#DCFCE7] text-[#166534]";
    default:
      return "";
  }
}
function statusClass(ststus?: Task["status"]) {
  switch (ststus) {
    case "Done":
      return "bg-[#DCFCE7] text-[#166534]";
    case "To Do":
      return "bg-[#FEF3C7] text-[#92400E]";
    case "In Progress":
      return "bg-[#DBEAFE] text-[#1E40AF]";
    default:
      return "";
  }
}