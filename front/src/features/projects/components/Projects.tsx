// types
import type { Project } from "#lib/api/types";
// hooks
import { useNavigate } from "@tanstack/react-router";
// Compoenentes
import { ProjectCard } from "./index";

// keycloak
import { keycloak } from "@/lib/auth/keycloak";
// Parametros
interface params {
  projects: Project[];
  onEdit: (p: Project) => void;
  onArchive: (p: Project) => void;
  onDelete: (p: Project) => void;
}

// Component
export function Projects({ projects, onEdit, onArchive, onDelete }: params) {
  const isAdmin = keycloak.hasRealmRole("Admin")
  const nav = useNavigate();
  const openProject = (p: Project) => {
    nav({ to: "/projects/$id", params: { id: p.id } })
  }
  return (
    <div className="flex flex-wrap gap-5 content-start">
      {projects
        ?.filter((p) => p.archivedAt === null)
        .map((p) => (
          <ProjectCard
            key={p.id}
            isAdmin={isAdmin}
            proyecto={p}
            title={p.name}
            description={p.description}
            miembros={1}
            tareasAbiertas={5}
            favourite={false}
            onOpen={openProject}
            onDelete={onDelete}
            onArchive={onArchive}
            onEdit={onEdit}
          />
        ))}
    </div>
  );
}
