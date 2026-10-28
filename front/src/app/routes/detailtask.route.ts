import { createRoute } from "@tanstack/react-router";
import { rootRoute } from "../router";

// PAGe
import { DetailTask } from "../layouts/index.layouts";

// ROUTE
export const DetailTaskRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/task/$id",
    component: DetailTask
})