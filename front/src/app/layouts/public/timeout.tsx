// Components
import { Button } from "@/components/index.comp";

// Router
import { Link } from "@tanstack/react-router";

// FORBIDDEN
export function TimeOut() {
  return (
    <div className="w-screen h-screen flex flex-col justify-center items-center">
      <h1>401 Sesion expirada</h1>
      <Button>
        <Link to="/">Volver</Link>
      </Button>
    </div>
  );
}
