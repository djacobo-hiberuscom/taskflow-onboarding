// COMPOENENTES
import { Badge, Button } from "@/components/index.comp";
import type { KeycloakTokenParsed } from "keycloak-js";

// KeyCloak
import {logout} from "@/lib/auth/keycloak"

// Parametros del compoenente
interface meParams {
    usuario: KeycloakTokenParsed | undefined
}

// COMPOENENTE
export function MeCard({usuario}: meParams) {
  return (
    <div className="flex flex-col gap-3 border p-5 rounded-xl bg-background justify-center">
      <div className="flex flex-col flex-1 gap-4 items-center">
        <div className="border w-35 h-35 rounded-full bg-primary flex justify-center items-center text-6xl text-white">
          {usuario?.name[0]}
        </div>
        <h1>{usuario?.preferred_username}</h1>
        <Badge>{usuario?.realm_access?.roles}</Badge>
        <h2>{usuario?.email}</h2>
      </div>
      <Button variant="outline">Editar perfil</Button>
      <Button
        variant="outline"
        className="border-red-300 text-red-800 hover:brightness-98 hover:border-red-300 hover:text-red-800"
        onClick={logout}
      >
        Cerar sesion
      </Button>
    </div>
  );
}
