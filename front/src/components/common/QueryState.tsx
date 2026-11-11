import { LoaderCircle, TriangleAlert } from "lucide-react";
import { ApiError } from "@/lib/api/client";

// Spinner de carga
export function Loading({ label = "Cargando..." }: { label?: string }) {
  return (
    <div className="flex flex-1 items-center justify-center gap-2 p-10 text-muted-foreground">
      <LoaderCircle className="size-5 animate-spin" />
      <span>{label}</span>
    </div>
  );
}

// Mensaje de error de una query
export function ErrorMessage({ error }: { error: unknown }) {
  const notFound = error instanceof ApiError && error.status === 404;
  const message = notFound
    ? "No se ha encontrado lo que buscas"
    : error instanceof Error
      ? error.message
      : "Ha ocurrido un error";

  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-2 p-10 text-center">
      <TriangleAlert className="size-8 text-red-500" />
      <h2 className="text-lg font-semibold">{notFound ? "404" : "Error"}</h2>
      <p className="text-muted-foreground">{message}</p>
    </div>
  );
}

// Uso: if (isLoading || error) return <QueryState isLoading={isLoading} error={error} />
export function QueryState({ isLoading, error }: { isLoading: boolean; error: unknown }) {
  if (isLoading) return <Loading />;
  return <ErrorMessage error={error} />;
}
