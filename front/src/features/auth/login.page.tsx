// Router
import { Link } from "@tanstack/react-router";

// Componentes
import { Button } from "@/components/index.comp";

// Keycloak
import { login } from "@/lib/auth/keycloak";


// FEAT
export function LoginPage() {
  return (
    <div className="flex flex-col justify-center items-center h-screen">
      <div className="border p-5 rounded-xl flex flex-col gap-4 justify-center items-center">
        <h1>TaskFlow</h1>
        <Button onClick={login}>Iniciar sesión</Button>
        <Link to="/register">Crear cuenta</Link>
      </div>
    </div>
  );
}
