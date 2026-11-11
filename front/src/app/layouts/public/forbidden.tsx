import { ErrorPage } from "./ErrorPage";

// 403
export function Forbidden() {
  return <ErrorPage message="403 · No tienes acceso" />;
}
