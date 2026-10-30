import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import { RouterProvider } from "@tanstack/react-router";
import { QueryClientProvider, QueryClient } from "@tanstack/react-query";
import { router } from "./app/router.tsx";
const queryClient = new QueryClient();

import {initKeycloak} from "@/lib/auth/keycloak.ts"

function render () {
  createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  </StrictMode>,
);
}

initKeycloak().catch(console.error).finally(render)