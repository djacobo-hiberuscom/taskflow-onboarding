import { useNavigate } from "@tanstack/react-router";
import { useAuth } from "@/hooks/use-auth";
import type { Project } from "@/lib/api/types";
import { ProjectCard } from "./Project-Card";

interface ProjectsProps {
  projects: Project[];
  onEdit: (p: Project) => void;
  onArchive: (p: Project) => void;
  onDelete: (p: Project) => void;
}

// Grid de tarjetas de proyecto
export function Projects({ projects, onEdit, onArchive, onDelete }: ProjectsProps) {
  const { isAdmin } = useAuth();
  const nav = useNavigate();

  if (projects.length === 0)
    return <p className="p-10 text-center text-muted-foreground">Todavía no hay proyectos</p>;

  return (
    <div className="flex flex-wrap gap-5 content-start">
      {projects.map((p) => (
        <ProjectCard
          key={p.id}
          proyecto={p}
          isAdmin={isAdmin}
          onOpen={() => nav({ to: "/projects/$id", params: { id: p.id } })}
          onEdit={onEdit}
          onArchive={onArchive}
          onDelete={onDelete}
        />
      ))}
    </div>
  );
}
