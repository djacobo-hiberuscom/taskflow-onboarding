import {
  createRootRoute,
  createRouter,
  Outlet,
} from '@tanstack/react-router'

// Routes
import { DashBoardRoute, ProjectsRoute, TasksRoute, ProfileRoute } from "./routes"
// Layouts
import { AppLayoutRoute } from "./routes"


export const rootRoute = createRootRoute({
    component: () => <Outlet />
})

const routeTree = rootRoute.addChildren([AppLayoutRoute.addChildren([DashBoardRoute, ProjectsRoute, TasksRoute, ProfileRoute])])

export const router = createRouter({routeTree})

declare module "@tanstack/react-router"{
    interface Register {
        router: typeof router
    }
}