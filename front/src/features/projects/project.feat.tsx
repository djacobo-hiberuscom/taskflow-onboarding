// Componentess

import {
  ProjectCard,
  AddButton,
  ArchivedButton,
} from "./components/index.comp";

// PROYECTOS
import PROYECTOS from "@/assets/tempdata/projects.json";

export function ProjectFeeat() {
  const projects = PROYECTOS;
  return (
    <div className="p-4 ">
      <div id="cards" className="flex flex-wrap gap-5">
        {projects.map((p) => (
          <ProjectCard
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
}
