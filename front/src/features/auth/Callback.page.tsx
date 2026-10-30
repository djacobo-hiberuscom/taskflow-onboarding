import { Button } from "@/components/ui/button";
import { login } from "@/lib/auth/keycloak";

export function CallbackPage() {
  return (
    <main>
      <p>No se pudo iniciar sesión.</p>
      <Button onClick={login}>Reintentar</Button>
    </main>
  );
}