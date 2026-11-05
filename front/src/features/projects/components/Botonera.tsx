// Compoenentes
import { ArchivedButton, AddButton } from "./index";
// keycloak
import { keycloak } from "@/lib/auth/keycloak";

// Parametros
interface params {
    openArchive: () => void,
    openNewProject: () => void
}


// Componente
export function Botonera({openArchive, openNewProject}: params) {
    const usuaio = keycloak.tokenParsed
  return (
    <div
      id="botonera"
      className="sticky bottom-4 mt-auto flex justify-end gap-2"
    >
      <ArchivedButton isAdmin={usuaio?.realm_access?.roles[0] === "Admin"} onClic={openArchive} />
      <AddButton onClic={openNewProject} />
    </div>
  );
}
