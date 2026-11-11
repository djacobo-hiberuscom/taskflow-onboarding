// Resumen de usuario que devuelve el back (miembros, asignados, creador...)
export interface UserSummary {
  id: string;
  displayName: string;
}

// GET /api/v1/me
export interface CurrentUser {
  id: string;
  displayName: string;
  email: string | null;
  roles: string[];
}
