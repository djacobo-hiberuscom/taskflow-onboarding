// Componentess
import { useState } from "react";
import {
  ProjectCard,
  AddButton,
  ArchivedButton,
  
} from "./components/index.comp";

// Popups
import { ArchivedPopUp, NewProjectPopup } from "./popups/index.popup";

// PROYECTOS
import PROYECTOS from "@/assets/tempdata/projects.json";
import { useNavigate } from "@tanstack/react-router";

// USER
import { keycloak } from "@/lib/auth/keycloak";

export function ProjectFeeat() {
  const isAdmin: boolean = keycloak.hasRealmRole("Admin")
  const [archivedOpen, setArcivedOpen] = useState(false)
  const [newProjectOpen, setNewProjectOpen] = useState(false)
  const nav = useNavigate()
  const projects = PROYECTOS;

  const openArchive = () => {
    setArcivedOpen(!archivedOpen)
  }
  const openNewProject = () => {
    setNewProjectOpen(!newProjectOpen)
  }
  return (
    
    <div className="p-4 flex flex-col gap-2">
      <ArchivedPopUp 
      isOpen={archivedOpen}
      onOpenChange={ (open) =>{
        if (!open) setArcivedOpen(false)
      }}/>
      <NewProjectPopup 
      isOpen={newProjectOpen}
      onOpenChange={(open) => {
        if (!open) setNewProjectOpen(false)
      }}/>

        <div className="flex flex-wrap gap-5">
          {projects.map((p) => (
            <ProjectCard
              key={p.id}
              isAdmin={isAdmin}
              id = {p.id}
              onClic={() =>  nav({to : "/projects/$id", params: {id: p.id}})}
              title={p.name}
              description={p.description}
              miembros={1}
              tareasAbiertas={5}
              favourite={false}
            />
          ))}
        </div>
        <div id="botonera" className="sticky bottom-4 flex justify-end gap-2">
          <ArchivedButton isAdmin={isAdmin} onClic={() => openArchive()} />
          <AddButton onClic={() => openNewProject()} />
        </div>
      </div>
  );  
    {/*
      <Drawer
      modal={false}
      disablePointerDismissal
      swipeDirection="right"
      open={selected !== null}
      onOpenChange={(open) =>  !open && setSelected(null)}
    >
      <ProjectDrawer project={selected} />
    </Drawer>
      */
     }  
  
}
