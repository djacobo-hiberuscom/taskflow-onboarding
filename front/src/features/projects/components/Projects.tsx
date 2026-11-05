// types
import type { Project } from "#lib/api/types"
// hooks
import { useNavigate } from "@tanstack/react-router";
// Compoenentes
import { ProjectCard } from "./index"

// keycloak
import { keycloak } from "@/lib/auth/keycloak";
// Parametros 
interface params{
    projects: Project[],
    onEdit: (p : Project) => void,
    onArchive: (p: Project) => void,
    onDelete: (p: Project) => void
}

// Component
export function Projects ({projects, onEdit, onArchive, onDelete}: params) {

    const nav = useNavigate()
    return (
        <div className="flex flex-wrap gap-5 content-start">
        {projects
          ?.filter((p) => p.archivedAt === null)
          .map((p) => (
            <ProjectCard
              key={p.id}
              isAdmin={keycloak.hasRealmRole("Admin")}
              id={p.id}
              onClic={() => nav({ to: "/projects/$id", params: { id: p.id } })}
              title={p.name}
              description={p.description}
              miembros={1}
              tareasAbiertas={5}
              favourite={false}
              onDelete={() => onDelete(p)}
              onArchive={() => onArchive(p)}
              onEdit={() => onEdit(p)}
            />
          ))}
      </div>
    )
}