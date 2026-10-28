import { rootRoute } from '../router';
import { createRoute } from "@tanstack/react-router";

// Page
import { AppLayout } from "../../app/layouts/index.layouts";

// ROUTE
export const DashBoardRoute = createRoute({
    getParentRoute:() => rootRoute,
    path: "/",
    component: AppLayout
})