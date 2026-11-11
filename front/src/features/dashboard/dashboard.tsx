import { CheckCircle2, CircleDashed, FolderKanban, ListTodo } from "lucide-react";
import { ChartAreaTasks } from "@/components";
import { useProjects } from "@/hooks/use-project";
import { useAllTask, useTaskCount } from "@/hooks/use-task";
import { StatCard } from "./components";

// FEAT: Panel
export function DashboardFeat() {
  // Solo necesitamos los totales -> pageSize 1
  const { data: proyectos } = useProjects({ pageSize: 1 });
  const { data: todo = 0 } = useTaskCount({ status: ["ToDo"] });
  const { data: inProgress = 0 } = useTaskCount({ status: ["InProgress"] });
  const { data: done = 0 } = useTaskCount({ status: ["Done"] });

  // tareas para el historico del grafico
  const {data: task = []} = useAllTask();


  // componente
  return (
    <div className="p-5 h-full flex flex-col gap-4">
      <div className="flex gap-5 justify-between">
        <StatCard name="Proyectos activos" number={proyectos?.totalCount ?? 0} icon={FolderKanban} />
        <StatCard name="Tareas abiertas" number={todo + inProgress} icon={ListTodo} />
        <StatCard name="Tareas en curso" number={inProgress} icon={CircleDashed} />
        <StatCard name="Tareas completadas" number={done} icon={CheckCircle2} />
      </div>
      <ChartAreaTasks tasks={task} />
    </div>
  );
}
