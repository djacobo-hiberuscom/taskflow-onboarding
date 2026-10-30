import { Link } from "@tanstack/react-router";
import { Button } from "@/components/ui/button";
import { login } from "@/lib/auth/keycloak";

export function LoginPage() {
  return (
    <main>
      <h1>TaskFlow</h1>
      <Button onClick={login}>Iniciar sesión</Button>
      <Link to="/register">Crear cuenta</Link>
    </main>
  );
}