// Router
import { Link } from "@tanstack/react-router";

// Componentes
import { Button } from "@/components/index.comp";

// Keycloak
import { register } from "@/lib/auth/keycloak";

// Feat
export function RegisterPage() {
  return (
    <div className="flex flex-col justify-center items-center h-screen">
      <div className="border p-5 rounded-xl flex flex-col gap-4 justify-center items-center">
        <h1>Crear cuenta</h1>
        <Button onClick={register}>Crear cuenta</Button>
        <Link to="/login">Volver</Link>
      </div>
    </div>
  );
}
