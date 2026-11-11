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

// Grid de tarjetas de proyecto (el empty state lo decide el padre)
export function Projects({ projects, onEdit, onArchive, onDelete }: ProjectsProps) {
  const { isAdmin } = useAuth();
  const nav = useNavigate();

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
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
