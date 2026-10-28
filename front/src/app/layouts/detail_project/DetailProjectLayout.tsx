import TAREAS from "@/assets/tempdata/tasks.json";
import PROYECTOS from "@/assets/tempdata/projects.json";
import { getUsernameById } from "@/tools/getUsername";
import { useNavigate, useParams } from "@tanstack/react-router";
import { TaskColum } from "@/components/common/tools/index.tool";
import { ArrowBigLeftDashIcon } from "lucide-react";

import {
  Button,
  Avatar,
  AvatarImage,
  DataTable,
  Input,
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerFooter,
  DrawerHeader,
  DrawerTitle,
} from "@/components/index.comp";
import { useState } from "react";
import type { Task } from "#lib/api/types";

export function DetailProject() {
  const { id } = useParams({ from: "/project/$id" });
  const [selectTask, setSelectTask] = useState<Task | null>(null);
  const nav = useNavigate();

  const navBack = () => {
    nav({
      to: "/",
    });
  };

  const projectTask = TAREAS.filter((t) => t.project_id === id);

  const proyecto = PROYECTOS.find((p) => p.id === id);
  if (!proyecto) {
    return <p>Proyecto no encontrado</p>;
  }

  return (
    <Drawer
      modal={false}
      disablePointerDismissal
      swipeDirection="right"
      open={selectTask !== null}
    >
      <div>
        <div
          className="absolute top-5 left-10 p-1 rounded-full  border shadow-black"
          onClick={navBack}
        >
          <ArrowBigLeftDashIcon />
        </div>

        <div className="flex flex-col items-center justify-center gap-8 p-5 border rounded-xl m-10 mt-15">
          <div className="p-5 rounded-md w-full border">
            <h1 className="text-3xl font-bold">{proyecto.name}</h1>
            <p> {proyecto.description}</p>
            <div className="flex flex-row gap-5">
              <h3>{getUsernameById(proyecto.created_by)}</h3>
              <h3>{proyecto.created_at}</h3>
              <h3>{proyecto.updated_at}</h3>
            </div>
          </div>

          <div
            id="tabla-task"
            className="flex flex-col overflow-hidden rounded-md border p-5 gap-4 w-full"
          >
            <Input placeholder="Buscar por nombre" />
            <DataTable
              columns={TaskColum}
              data={projectTask}
              onClic={(t) => setSelectTask(t)}
            />
          </div>
        </div>
      </div>
      <DrawerContent>
        <DrawerHeader>
          <DrawerTitle>{proyecto.name}</DrawerTitle>
        </DrawerHeader>
        <div className="flex-1 p-4">
          <h1 className="text-3xl font-bold">{selectTask?.title}</h1>
          <div className="flex gap-2 items-center">
            <Avatar>
              <AvatarImage src="https://www.svgrepo.com/show/452030/avatar-default.svg" />
            </Avatar>
            <span>{selectTask && getUsernameById(selectTask.assigned_id)}</span>
          </div>
        </div>
        <DrawerFooter>
          <DrawerClose
            render={<Button onClick={() => setSelectTask(null)}>Close</Button>}
          />
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  );
}
