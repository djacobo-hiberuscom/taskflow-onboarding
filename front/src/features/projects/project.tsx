import { useState } from "react";
// Componentess
import { Projects, Botonera, PopupContainer } from "./components";

// USER
import { useProjects } from "#hooks/use-project";

import { ApiError } from "#lib/api/client";
import { NotFound } from "@/app/layouts/index";
import {type Project } from "#lib/api/types";

// Types

//FEAT
export function ProjectFeeat() {
  const { data, isLoading, error } = useProjects(); // Proyectos

  const [popup, setPopup] = useState<string | null>(null); // Abrir popup
  const [selected, setSelected] = useState<Project | null>(null); // selecionado

  const projects = data?.items ?? [];

  if (isLoading) return <h1>Cargando...</h1>;
  if (error instanceof ApiError && error.status === 404) return <NotFound />;
  if (error) return <h1>{error.message}</h1>;
  
  return (
    <div className="p-4 flex flex-col flex-1 gap-2">
      <PopupContainer
        popup={popup}
        close={(open) => {
          if (!open) setPopup(null);
        }}
        proyecto={selected ?? undefined}
      />

      <Projects
        projects={projects}
        onArchive={(p) => {
          setSelected(p);
          setPopup("Archive");
        }}
        onDelete={(p) => {
          setSelected(p);
          setPopup("Delete");
        }}
        onEdit={(p) => {
          setSelected(p);
          setPopup("Edit");
        }}
      />

      <Botonera
        openNewProject={() => setPopup("New")}
        openArchive={() => setPopup("Archive")}
      />
    </div>
  );
}
