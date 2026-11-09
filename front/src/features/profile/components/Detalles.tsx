// Kaycloak
import type { KeycloakTokenParsed } from "keycloak-js";

// Parametros del compoenene
interface param {
    usuario: KeycloakTokenParsed | undefined
}

// COMPONENTE
export function DetallesMe({usuario}: param) {
  return (
    <div
      id="detalles"
      className="bg-background p-5 flex flex-col gap-3 border rounded-xl"
    >
      <div className="flex gap-2">
        <h1 className="flex flex-1">Detalles</h1>
      </div>
      <div className="flex gap-2">
        <h2 className="flex flex-1">Nombre Completo</h2>
        <span>{usuario?.name}</span>
      </div>
      <div className="flex gap-2">
        <h2 className="flex flex-1">Correo electronico</h2>
        <span>{usuario?.email}</span>
      </div>
      <div className="flex gap-2">
        <h2 className="flex flex-1">Role</h2>
        <span>{usuario?.realm_access?.roles}</span>
      </div>
      <div className="flex gap-2">
        <h2 className="flex flex-1">telefolo</h2>
        <span>null</span>
      </div>
    </div>
  );
}
