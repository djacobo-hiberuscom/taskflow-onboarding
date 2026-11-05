import { Link } from "@tanstack/react-router";
import { Button } from "@/components/ui/button";
import { register } from "@/lib/auth/keycloak";

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
