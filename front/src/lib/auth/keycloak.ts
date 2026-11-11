import Keycloak from "keycloak-js";
import { config } from "@/lib/config";

const origin = window.location.origin;

export const keycloak = new Keycloak({
  url: config.keycloakUrl,
  realm: config.keycloakRealm,
  clientId: config.keycloakClientId,
});

export const initKeycloak = () =>
  keycloak.init({
    onLoad: "check-sso",
    pkceMethod: "S256",
    checkLoginIframe: false,
    silentCheckSsoRedirectUri: `${origin}/silent-check-sso.html`,
  });

// Acciones de sesión
export const login = () =>
  keycloak.login({ redirectUri: `${origin}/callback` });
export const register = () =>
  keycloak.register({ redirectUri: `${origin}/callback` });
export const logout = () => keycloak.logout({ redirectUri: `${origin}/` });

// Roles
export const ADMIN_ROLE = "Admin";
export const isAdmin = () => keycloak.hasRealmRole(ADMIN_ROLE);

// Token siempre fresco para las peticiones
export async function getAccessToken() {
  try {
    await keycloak.updateToken(30);
    return keycloak.token!;
  } catch {
    keycloak.clearToken();
    window.location.assign("/login");
    throw new Error("Sesión expirada");
  }
}
