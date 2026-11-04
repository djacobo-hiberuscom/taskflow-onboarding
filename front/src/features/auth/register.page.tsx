import { Link } from "@tanstack/react-router";
import { Button } from "@/components/ui/button";
import { register } from "@/lib/auth/keycloak";

export function RegisterPage() {
  return (
    <main>
      <h1>Crear cuenta</h1>
      <Button onClick={register}>Crear cuenta</Button>
      <Link to="/login">Volver</Link>
    </main>
  );
}