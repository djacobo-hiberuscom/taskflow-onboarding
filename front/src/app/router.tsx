import { createRootRoute, createRouter, Outlet } from "@tanstack/react-router";

// Routes
import {
  DashBoardRoute,
  ProjectsRoute,
  TasksRoute,
  ProfileRoute,
  DetailProjectRoute,
  LoginRoute,
  RegisterRoute,
  CallbackRoute,

  NotFoundRoute,
  ForbiddenRoute,
  TimeOutRoute
} from "./routes";
// Layouts
import { AppLayoutRoute } from "./routes";

export const rootRoute = createRootRoute({
  component: () => <Outlet />,
});

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

export const router = createRouter({ routeTree });

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
