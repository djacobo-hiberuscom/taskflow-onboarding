import { ErrorPage } from "./ErrorPage";

// 404
export function NotFound() {
  return <ErrorPage message="404 · Algo ha salido mal" />;
}
