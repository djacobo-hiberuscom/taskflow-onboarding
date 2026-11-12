import { Link } from "@tanstack/react-router";
import { ArrowLeft } from "lucide-react";
import { cn } from "@/lib/utils";
// Volver al listado de proyectos
export function BackButton() {
  return (
    <Link
      to="/projects"
      aria-label="Volver a proyectos"
      className={cn(
        "absolute left-4 top-4 z-10 flex h-10 w-10 items-center justify-center rounded-lg bg-button text-muted-foreground shadow-md backdrop-blur-sm transition-colors hover:bg-button/90 hover:text-foreground sm:left-6 sm:top-6",
      )}
    >
      <ArrowLeft />
    </Link>
  );
}
