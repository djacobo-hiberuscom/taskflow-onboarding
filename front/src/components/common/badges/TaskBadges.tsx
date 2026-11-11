import { Badge } from "@/components/ui/badge";
import type { TaskPriority, TaskStatus } from "@/lib/api/types";
import {
  TASK_PRIORITY_BADGE_CLASS,
  TASK_PRIORITY_LABEL,
  TASK_PRIORITY_LEVEL,
  TASK_STATUS_BADGE_CLASS,
  TASK_STATUS_DOT_CLASS,
  TASK_STATUS_LABEL,
} from "@/lib/constants/task";
import { cn } from "@/lib/utils";

export function TaskStatusBadge({ status }: { status: TaskStatus }) {
  return (
    <Badge
      variant="outline"
      className={cn("h-6 gap-1.5 px-2 text-xs", TASK_STATUS_BADGE_CLASS[status])}
    >
      <span className={cn("size-1.5 rounded-full", TASK_STATUS_DOT_CLASS[status])} />
      {TASK_STATUS_LABEL[status]}
    </Badge>
  );
}

const BAR_HEIGHTS = ["h-1", "h-1.5", "h-2.5"];

// Icono de barras de señal (1-3 rellenas)
function PriorityBars({ level }: { level: number }) {
  return (
    <span className="flex h-2.5 items-end gap-px" aria-hidden>
      {BAR_HEIGHTS.map((h, i) => (
        <span
          key={h}
          className={cn("w-0.5 rounded-sm bg-current", h, i >= level && "opacity-25")}
        />
      ))}
    </span>
  );
}

export function TaskPriorityBadge({ priority }: { priority: TaskPriority }) {
  return (
    <Badge
      variant="outline"
      className={cn("h-6 gap-1.5 rounded-md px-2 text-xs", TASK_PRIORITY_BADGE_CLASS[priority])}
    >
      <PriorityBars level={TASK_PRIORITY_LEVEL[priority]} />
      {TASK_PRIORITY_LABEL[priority]}
    </Badge>
  );
}
