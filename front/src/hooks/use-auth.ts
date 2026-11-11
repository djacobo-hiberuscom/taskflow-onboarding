import { isAdmin, keycloak, logout } from "@/lib/auth/keycloak";

// Datos de sesión del token de Keycloak
export function useAuth() {
  const user = keycloak.tokenParsed;
  return {
    user,
    roles: user?.realm_access?.roles ?? [],
    isAdmin: isAdmin(),
    logout,
  };
}
