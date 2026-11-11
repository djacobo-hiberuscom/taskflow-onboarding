import { Badge } from "@/components/ui/badge";
import type { TaskPriority, TaskStatus } from "@/lib/api/types";
import {
  TASK_PRIORITY_BADGE_CLASS,
  TASK_PRIORITY_LABEL,
  TASK_STATUS_BADGE_CLASS,
  TASK_STATUS_LABEL,
} from "@/lib/constants/task";

export function TaskStatusBadge({ status }: { status: TaskStatus }) {
  return <Badge className={TASK_STATUS_BADGE_CLASS[status]}>{TASK_STATUS_LABEL[status]}</Badge>;
}

export function TaskPriorityBadge({ priority }: { priority: TaskPriority }) {
  return (
    <Badge className={TASK_PRIORITY_BADGE_CLASS[priority]}>{TASK_PRIORITY_LABEL[priority]}</Badge>
  );
}
