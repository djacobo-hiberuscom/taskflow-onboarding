import {
  createRootRoute,
  createRouter,
  Outlet,
} from '@tanstack/react-router'

// Routes
import { DashBoardRoute, DetailProjectRoute } from "./routes/index.route"

// Pages

export const rootRoute = createRootRoute({
    component: () => <Outlet />
})

const routeTree = rootRoute.addChildren([DashBoardRoute, DetailProjectRoute])

export const router = createRouter({routeTree})

declare module "@tanstack/react-router"{
    interface Register {
        router: typeof router
    }
}