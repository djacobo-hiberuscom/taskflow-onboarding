// componentes
import {Header, DetallesMe} from "./index"

// Tarjeta con avatar, usuario y cerrar sesión
export function MeCard() {
  return (
    <div className="overflow-hidden rounded-2xl border bg-card">
      <Header />
      <DetallesMe />
    </div>
  );
}
