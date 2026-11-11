import { Link } from "@tanstack/react-router";
import { cn } from "cn";
// iconos
import type { LucideIcon } from "lucide-react";
import { ChevronRight, CircleCheck, Clock, FolderKanban } from "lucide-react";

// hooks
import { useMe } from "@/hooks/use-users";
import { useTaskCount } from "@/hooks/use-task";
import { useProjects } from "@/hooks/use-project";

interface ProfileStatsCardParams {
  label: string,
  value: number | undefined,
  icon: LucideIcon,
  tone: string,
  to: "/tasks" | "/projects",
  linkLabel: string
}

// estats del usuario 
function ProfileStatsCard ({label, value, icon: Icon, tone, to, linkLabel}: ProfileStatsCardParams) {
  return (
    <div className="flex flex-col gap-3 rounded-2xl border bg-card p-4">
      <div className="flex items-center gap-3">
        <div className={cn("flex size-9 items-center justify-center rounded-lg", tone)}>
          <Icon className="size-5" />
        </div>
        <span className="text-sm text-muted-foreground">{label}</span>
      </div>
      <p className="text-3xl font-semibold">{value ?? "—"}</p>
      <Link to={to} className="flex items-center gap-1 text-sm text-primary hover:underline">
        {linkLabel} <ChevronRight className="size-4" />
      </Link>
    </div>
  )
}

export function StatsMe() {
  const { data: me } = useMe();
  const ready = !!me; 

  const { data: completadas } = useTaskCount(
    { assigneeId: me?.id, status: ["Done"] },
    { enabled: ready },
  );
  const { data: pendientes } = useTaskCount(
    { assigneeId: me?.id, status: ["ToDo", "InProgress"] },
    { enabled: ready },
  );
  const { data: proyectos } = useProjects({ pageSize: 1 }); // solo nos interesa totalCount

  return (
    <div className="grid gap-4 sm:grid-cols-3">
      <ProfileStatsCard
        label="Tareas completadas"
        value={completadas}
        icon={CircleCheck}
        tone="bg-success text-success-foreground"
        to="/tasks"
        linkLabel="Ver mis tareas"
      />
      <ProfileStatsCard
        label="Tareas pendientes"
        value={pendientes}
        icon={Clock}
        tone="bg-warning text-warning-foreground"
        to="/tasks"
        linkLabel="Ver mis tareas"
      />
      <ProfileStatsCard
        label="Proyectos"
        value={proyectos?.totalCount}
        icon={FolderKanban}
        tone="bg-accent text-accent-foreground"
        to="/projects"
        linkLabel="Ver proyectos"
      />
    </div>
  );
}