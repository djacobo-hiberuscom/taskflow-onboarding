// COMPOENENTES

import { DetallesMe, MeCard, StatsMe } from "./components/index";
// Keycloak
import { keycloak } from "@/lib/auth/keycloak";
import { LastChanges } from "./components/LastChanges";

// Compoenente
export function MyProfileFeat() {
  // USUAIRO LOGEADO
  const usuario = keycloak.tokenParsed;
  return (
    <div className="grid grid-cols-3 gap-3 p-5">
      
      <MeCard usuario={usuario}/>

      <div className="flex flex-col gap-4">
        <DetallesMe usuario={usuario}/>
        <StatsMe />
      </div>
      <LastChanges />
    </div>
  );
}
