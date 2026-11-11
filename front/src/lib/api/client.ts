import { config } from "@/lib/config";
import { getAccessToken } from "@/lib/auth/keycloak";

// Error HTTP del back (problem = body ProblemDetails si lo hay)
export class ApiError extends Error {
  status: number;
  problem?: unknown;
  constructor(status: number, problem?: unknown) {
    super(`HTTP ${status}`);
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }
}

// fetch autenticado contra el back
export async function apiFetch<T = void>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("Authorization", `Bearer ${await getAccessToken()}`);
  if (init.body) headers.set("Content-Type", "application/json");

  const res = await fetch(`${config.apiBaseUrl}${path}`, { ...init, headers });

  if (!res.ok) {
    const problem = await res.json().catch(() => undefined);
    throw new ApiError(res.status, problem);
  }
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

const json = (body: unknown) => (body === undefined ? undefined : JSON.stringify(body));

// Helpers para no repetir method/body en cada endpoint
export const api = {
  get: <T>(path: string) => apiFetch<T>(path),
  post: <T = void>(path: string, body?: unknown) =>
    apiFetch<T>(path, { method: "POST", body: json(body) }),
  put: <T = void>(path: string, body?: unknown) =>
    apiFetch<T>(path, { method: "PUT", body: json(body) }),
  delete: <T = void>(path: string) => apiFetch<T>(path, { method: "DELETE" }),
};

// Construye un query string ignorando undefined y repitiendo arrays (?status=A&status=B)
export function toQueryString(params: object): string {
  const qs = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null) continue;
    if (Array.isArray(value)) value.forEach((v) => qs.append(key, String(v)));
    else qs.set(key, String(value));
  }
  return qs.toString();
}
