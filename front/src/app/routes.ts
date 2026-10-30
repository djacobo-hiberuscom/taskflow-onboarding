import { rootRoute } from "./router";
import { createRoute, redirect } from "@tanstack/react-router";

import { keycloak } from "@/lib/auth/keycloak";
import {
  LoginPage,
  RegisterPage,
  CallbackPage,
} from "@/features/index.featues";

// Layout
import { AppLayout } from "@/app/layouts/index.layouts";

// FEATURES
import {
  DashboardFeat,
  ProjectFeeat,
  TaskFeat,
  MyProfileFeat,
  DetailProject,
} from "@/features/index.featues";

// LAYOUT
export const AppLayoutRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: "app",
  beforeLoad: ({location}) => {
    if (keycloak.authenticated) return
    sessionStorage.setItem("returnTo", location.href)
    throw redirect({to: "/login"})
  },
  component: AppLayout,
});

// LOGIN
export const LoginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/login",
  
  component: LoginPage,
});
// REGISTER
export const RegisterRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/register",
  component: RegisterPage,
});
// CallbackRoute
export const CallbackRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "callback",
  beforeLoad: () => {
    if (!keycloak.authenticated) return;
    const href = sessionStorage.getItem("returnTo") ?? "/";
    sessionStorage.removeItem("returnTo");
    throw redirect({ href, replace: true });
  },
  component: CallbackPage,
});

// ROUTES

// DASHBOARD
export const DashBoardRoute = createRoute({
  getParentRoute: () => AppLayoutRoute,
  path: "/",
  component: DashboardFeat,
});

// PROJECT
export const ProjectsRoute = createRoute({
  getParentRoute: () => AppLayoutRoute,
  path: "/projects",
  component: ProjectFeeat,
});

// DETAIL PROJECT
export const DetailProjectRoute = createRoute({
  getParentRoute: () => AppLayoutRoute,
  path: "/projects/$id",
  component: DetailProject,
});

// TASK
export const TasksRoute = createRoute({
  getParentRoute: () => AppLayoutRoute,
  path: "/tasks",
  component: TaskFeat,
});

// PROFILE
export const ProfileRoute = createRoute({
  getParentRoute: () => AppLayoutRoute,
  path: "/me",
  component: MyProfileFeat,
});
