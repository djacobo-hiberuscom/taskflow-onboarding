import { Link } from "@tanstack/react-router";
import { ArrowBigLeftDashIcon } from "lucide-react";

// Volver al listado de proyectos
export function BackButton() {
  return (
    <Link
      to="/projects"
      aria-label="Volver a proyectos"
      className="absolute top-3 left-17 p-1 rounded-full border z-50 hover:bg-muted"
    >
      <ArrowBigLeftDashIcon />
    </Link>
  );
}
