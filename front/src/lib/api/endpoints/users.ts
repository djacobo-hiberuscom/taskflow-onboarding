import { api } from "../client";
import type { CurrentUser, UserSummary } from "../types";

// Llamadas HTTP de usuarios
export const usersApi = {
  me: () => api.get<CurrentUser>("/api/v1/me"),
  assignable: () => api.get<UserSummary[]>("/api/v1/users/assignable"),
};
