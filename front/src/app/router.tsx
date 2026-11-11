import { createRouter } from "@tanstack/react-router";

import { NotFound } from "@/app/layouts";
import {
  rootRoute,
  // Públicas
  LoginRoute,
  RegisterRoute,
  CallbackRoute,
  // Errores
  ForbiddenRoute,
  NotFoundRoute,
  TimeOutRoute,
  // App + feats
  AppLayoutRoute,
  DashBoardRoute,
  ProjectsRoute,
  DetailProjectRoute,
  TasksRoute,
  ProfileRoute,
} from "./routes";

// Árbol de rutas
const routeTree = rootRoute.addChildren([
  LoginRoute,
  RegisterRoute,
  CallbackRoute,
  ForbiddenRoute,
  NotFoundRoute,
  TimeOutRoute,
  AppLayoutRoute.addChildren([
    DashBoardRoute,
    ProjectsRoute,
    DetailProjectRoute,
    TasksRoute,
    ProfileRoute,
  ]),
]);

export const router = createRouter({
  routeTree,
  defaultNotFoundComponent: NotFound,
});

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
