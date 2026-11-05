// Componentess
import { useState } from "react";
import {
  ProjectCard,
  AddButton,
  ArchivedButton,
} from "./components";

// Popups
import {
  ArchiveConfirmPopUp,
  ArchivedPopUp,
  DeleteConfirmPopUp,
  EditProjectPopUp,
  NewProjectPopup,
} from "../../components/common/popup";

// PROYECTOS
import { useNavigate } from "@tanstack/react-router";

// USER
import { keycloak } from "@/lib/auth/keycloak";
import { useProjects } from "#hooks/use-project";
import { ApiError } from "#lib/api/client";
import { NotFound } from "..";
import type { Project } from "#lib/api/types";

export function ProjectFeeat() {
  const { data, isLoading, error } = useProjects();
  const isAdmin: boolean = keycloak.hasRealmRole("Admin");

  const [archivedOpen, setArcivedOpen] = useState(false);
  const [newProjectOpen, setNewProjectOpen] = useState(false);

  const [toEdit, setToEdit] = useState<Project | null>(null); // popUp de aeditar proyecto
  const [toDelete, setToDelete] = useState<Project | null>(null);
  const [toArchive, setToArchive] = useState<Project | null>(null);

  const nav = useNavigate();

  if (isLoading) return <h1>Cargando...</h1>;
  if (error instanceof ApiError && error.status === 404) return <NotFound />;
  if (error) return <h1>{error.message}</h1>;

  const projects = data?.items ?? [];

  const openArchive = () => {
    setArcivedOpen(!archivedOpen);
  };
  const openNewProject = () => {
    setNewProjectOpen(!newProjectOpen);
  };
  return (
    <div className="p-4 flex flex-col flex-1 gap-2">
      <ArchivedPopUp
        isOpen={archivedOpen}
        onOpenChange={(open) => {
          if (!open) setArcivedOpen(false);
        }}
      />
      <NewProjectPopup
        isOpen={newProjectOpen}
        onOpenChange={(open) => {
          if (!open) setNewProjectOpen(false);
        }}
      />
      <DeleteConfirmPopUp
        isOpen={toDelete !== null}
        proyecto={toDelete ?? undefined}
        onOpenChange={(open) => {
          if (!open) setToDelete(null);
        }}
      />
      <ArchiveConfirmPopUp
        isOpen={toArchive !== null}
        proyecto={toArchive ?? undefined}
        onOpenChange={(open) => {
          if (!open) setToArchive(null);
        }}
        onArchived={() => nav({ to: "/projects", replace: true })}
      />
      <EditProjectPopUp
        isOpen={toEdit !== null}
        onOpenChange={(open) => {
          if (!open) setToEdit(null);
        }}
        proyecto={toEdit}
      />

      <div className="flex flex-wrap gap-5 content-start">
        {projects
          ?.filter((p) => p.archivedAt === null)
          .map((p) => (
            <ProjectCard
              key={p.id}
              isAdmin={isAdmin}
              id={p.id}
              onClic={() => nav({ to: "/projects/$id", params: { id: p.id } })}
              title={p.name}
              description={p.description}
              miembros={1}
              tareasAbiertas={5}
              favourite={false}
              onDelete={() => setToDelete(p)}
              onArchive={() => setToArchive(p)}
              onEdit={() => setToEdit(p)}
            />
          ))}
      </div>
      <div
        id="botonera"
        className="sticky bottom-4 mt-auto flex justify-end gap-2"
      >
        <ArchivedButton isAdmin={isAdmin} onClic={() => openArchive()} />
        <AddButton onClic={() => openNewProject()} />
      </div>
    </div>
  );
}
