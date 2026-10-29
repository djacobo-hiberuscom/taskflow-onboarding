// Componentess
import {
  ProjectCard,
  AddButton,
  ArchivedButton,
} from "./components/index.comp";

// Popups
//import { NewProjectPopUp, EditProjectPopUp } from "./popups/index.popup";

// PROYECTOS
import PROYECTOS from "@/assets/tempdata/projects.json";
import { useNavigate } from "@tanstack/react-router";

export function ProjectFeeat() {
// DRAWER   const [selected, setSelected] = useState<Project | null>(null);
  const nav = useNavigate()
  const projects = PROYECTOS;
  return (
    <div className="p-4 flex flex-col gap-2">
        <div className="flex flex-wrap gap-5">
          {projects.map((p) => (
            <ProjectCard
              key={p.id}
              // setSelected(p.id === selected?.id ? null : p)
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
          <ArchivedButton onClic={() => {}} />
          <AddButton onClic={() => {}} />
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
