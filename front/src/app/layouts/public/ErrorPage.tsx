import { Link } from "@tanstack/react-router";
import { Button } from "@/components";

// Pantalla completa de error con botón para volver al inicio
export function ErrorPage({ message }: { message: string }) {
  return (
    <div className="w-screen h-screen flex flex-col justify-center items-center gap-4">
      <h1>{message}</h1>
      <Button nativeButton={false} render={<Link to="/" />}>
        Volver
      </Button>
    </div>
  );
}
