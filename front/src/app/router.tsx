// Router
import { createRootRoute, createRouter, Outlet } from "@tanstack/react-router";

// Routes
import {
  // Layouts
  AppLayoutRoute,
  LoginRoute,
  RegisterRoute,
  CallbackRoute,
  NotFoundRoute,
  ForbiddenRoute,
  TimeOutRoute,
  // Feat
  DashBoardRoute,
  ProjectsRoute,
  TasksRoute,
  ProfileRoute,
  DetailProjectRoute,
} from "./routes";

export const rootRoute = createRootRoute({
  component: () => <Outlet />,
});

// Arbol de rutas
const routeTree = rootRoute.addChildren([
  LoginRoute,
  RegisterRoute,
  CallbackRoute,
  NotFoundRoute,
  ForbiddenRoute,
  TimeOutRoute,
  AppLayoutRoute.addChildren([
    DashBoardRoute,
    ProjectsRoute,
    DetailProjectRoute,
    TasksRoute,
    ProfileRoute,
  ]),
]);

// Router
export const router = createRouter({ routeTree });

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
