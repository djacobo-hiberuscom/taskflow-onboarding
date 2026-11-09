// User sumary
export interface UserSummary {
  id: string;
  displayName: string;
}

// USER
export interface User {
  id: string;
  keycloak_sub: string;
  display_name: string;
  email: string;
  created_at: Date;
}
