import {
  createRootRoute,
  createRouter,
  Outlet,
} from '@tanstack/react-router'

// import {  } from "./routes/index.routes"

export const rootRoute = createRootRoute({
    component: () => <Outlet />
})

const routeTree = rootRoute.addChildren([])

export const router = createRouter({routeTree})

declare module "@tanstack/react-router"{
    interface Register {
        router: typeof router
    }
}