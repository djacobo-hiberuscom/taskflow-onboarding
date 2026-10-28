import {rootRoute} from "../router"
import {createRoute} from "@tanstack/react-router"

// page
import {DetailProject} from "@/app/layouts/detail_project/DetailProjectLayout"

// ROUTE
export const DetailProjectRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/project/$id",
    component: DetailProject
})