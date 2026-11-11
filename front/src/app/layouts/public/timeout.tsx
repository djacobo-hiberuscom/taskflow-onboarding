import { ErrorPage } from "./ErrorPage";

// 401
export function TimeOut() {
  return <ErrorPage message="401 · Sesión expirada" />;
}
