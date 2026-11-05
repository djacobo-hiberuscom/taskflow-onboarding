import { useState } from "react";
// Componentess
import { Projects, Botonera } from "./components";

// Popups
import {
  ArchiveConfirmPopUp,
  ArchivedPopUp,
  DeleteConfirmPopUp,
  EditProjectPopUp,
  NewProjectPopup,
} from "@/components/common/popup";

// PROYECTOS
import { useNavigate } from "@tanstack/react-router";

// USER
import { useProjects } from "#hooks/use-project";
import { ApiError } from "#lib/api/client";
import { NotFound } from "@/app/layouts/index";
import type { Project } from "#lib/api/types";

//FEAT
export function ProjectFeeat() {
  const { data, isLoading, error } = useProjects(); // Proyectos

  const [archivedOpen, setArcivedOpen] = useState(false); // Abrir popup de archivados
  const [newProjectOpen, setNewProjectOpen] = useState(false); // Abrir pop up de newproject
  const [toEdit, setToEdit] = useState<Project | null>(null); // popUp de aeditar proyecto
  const [toDelete, setToDelete] = useState<Project | null>(null); // popup de deleteconfirm
  const [toArchive, setToArchive] = useState<Project | null>(null); // popup de archive confirm

  const nav = useNavigate();
  const projects = data?.items ?? []
  if (isLoading) return <h1>Cargando...</h1>;
  if (error instanceof ApiError && error.status === 404) return <NotFound />;
  if (error) return <h1>{error.message}</h1>;

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

  
      <Projects
        projects={projects}
        onArchive={setToArchive}
        onDelete={setToDelete}
        onEdit={setToEdit}
      />

      <Botonera
        openNewProject={() => setNewProjectOpen(!newProjectOpen)}
        openArchive={() => setArcivedOpen(!archivedOpen)}
      />
    </div>
  );
}
