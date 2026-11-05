// Compoenentes
import { Button } from "@/components/index.comp";

// KeyCloak
import { login } from "@/lib/auth/keycloak";


// FEAT
export function CallbackPage() {
  return (
    <main>
      <p>No se pudo iniciar sesión.</p>
      <Button onClick={login}>Reintentar</Button>
    </main>
  );
}