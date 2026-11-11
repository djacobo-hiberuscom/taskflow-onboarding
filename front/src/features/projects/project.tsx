import { useState } from "react";
import { QueryState } from "@/components/index.comp";
import { useProjects } from "@/hooks/use-project";
import type { Project } from "@/lib/api/types";
import { Botonera, PopupContainer, Projects, type ProjectsPopup } from "./components";

// FEAT: Proyectos
export function ProjectsFeat() {
  const { data, isLoading, error } = useProjects();

  const [popup, setPopup] = useState<ProjectsPopup | null>(null);
  const [selected, setSelected] = useState<Project | undefined>(undefined);

  // Abre un popup sobre un proyecto concreto
  const openFor = (type: ProjectsPopup) => (p: Project) => {
    setSelected(p);
    setPopup(type);
  };

  if (isLoading || error) return <QueryState isLoading={isLoading} error={error} />;

  return (
    <div className="p-4 flex flex-col flex-1 gap-2">
      <PopupContainer popup={popup} proyecto={selected} close={() => setPopup(null)} />

      <Projects
        projects={data?.items ?? []}
        onEdit={openFor("edit")}
        onArchive={openFor("archive")}
        onDelete={openFor("delete")}
      />

      <Botonera
        openNewProject={() => setPopup("new")}
        openArchived={() => setPopup("archived-list")}
      />
    </div>
  );
}
