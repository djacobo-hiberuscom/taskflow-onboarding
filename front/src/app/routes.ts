import { createRootRoute, createRoute, Outlet, redirect } from "@tanstack/react-router";

import { keycloak } from "@/lib/auth/keycloak";

// Layouts
import {
  AppLayout,
  CallbackLayout,
  Forbidden,
  LoginLayout,
  NotFound,
  RegisterLayout,
  TimeOut,
} from "@/app/layouts";

// Features
import {
  DashboardFeat,
  DetailProjectFeat,
  MyProfileFeat,
  ProjectsFeat,
  TaskFeat,
} from "@/features";

// =====================================================
// ROOT
// =====================================================
export const rootRoute = createRootRoute({
  component: Outlet,
});

// =====================================================
// PÚBLICAS (sin sesión)
// =====================================================
export const LoginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/login",
  component: LoginLayout,
});

export const RegisterRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/register",
  component: RegisterLayout,
});

// Vuelta de Keycloak: si hay sesión, redirige a donde estaba el usuario
export const CallbackRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/callback",
  beforeLoad: () => {
    if (!keycloak.authenticated) return;
    const href = sessionStorage.getItem("returnTo") ?? "/";
    sessionStorage.removeItem("returnTo");
    throw redirect({ href, replace: true });
  },
  component: CallbackLayout,
});

// =====================================================
// ERRORES
// =====================================================
export const ForbiddenRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/403",
  component: Forbidden,
});

export const NotFoundRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/404",
  component: NotFound,
});

export const TimeOutRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/timeout",
  component: TimeOut,
});

// =====================================================
// APP (layout con sidebar, requiere sesión)
// =====================================================
export const AppLayoutRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: "app",
  beforeLoad: ({ location }) => {
    if (keycloak.authenticated) return;
    sessionStorage.setItem("returnTo", location.href);
    throw redirect({ to: "/login" });
  },
  component: AppLayout,
});

// ---------- FEATS (se pintan dentro del <Outlet /> del AppLayout) ----------
export const DashBoardRoute = createRoute({
  getParentRoute: () => AppLayoutRoute,
  path: "/",
  component: DashboardFeat,
});

export const ProjectsRoute = createRoute({
  getParentRoute: () => AppLayoutRoute,
  path: "/projects",
  component: ProjectsFeat,
});

export const DetailProjectRoute = createRoute({
  getParentRoute: () => AppLayoutRoute,
  path: "/projects/$id",
  component: DetailProjectFeat,
});

export const TasksRoute = createRoute({
  getParentRoute: () => AppLayoutRoute,
  path: "/tasks",
  component: TaskFeat,
});

export const ProfileRoute = createRoute({
  getParentRoute: () => AppLayoutRoute,
  path: "/me",
  component: MyProfileFeat,
});
