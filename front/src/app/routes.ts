import { rootRoute } from "./router";
import { createRoute } from "@tanstack/react-router";

// Layout
import {AppLayout} from "@/app/layouts/index.layouts"

// FEATURES
import {DashboardFeat, ProjectFeeat, TaskFeat, MyProfileFeat} from "@/features/index.featues"

// LAYOUT
export const AppLayoutRoute = createRoute({
    getParentRoute: () => rootRoute,
    id: "app",
    component: AppLayout
})

// ROUTES
// DASHBOARD
export const DashBoardRoute = createRoute({
    getParentRoute: () => AppLayoutRoute,
    path: "/",
    component: DashboardFeat
})

// PROJECT
export const ProjectsRoute = createRoute({
    getParentRoute: () => AppLayoutRoute,
    path: "/projects",
    component: ProjectFeeat
})

// TASK
export const TasksRoute = createRoute({
    getParentRoute: () => AppLayoutRoute,
    path: "/tasks",
    component: TaskFeat
})

// PROFILE
export const ProfileRoute = createRoute({
    getParentRoute: () => AppLayoutRoute,
    path: "/me",
    component: MyProfileFeat
})