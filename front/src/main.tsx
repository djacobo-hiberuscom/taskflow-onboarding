import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "@tanstack/react-router";

import "./index.css";
import { router } from "@/app/router";
import { Toaster } from "@/components/ui/toast";
import { initKeycloak } from "@/lib/auth/keycloak";
import { queryClient } from "@/lib/query-client";

function render() {
  createRoot(document.getElementById("root")!).render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <Toaster timeout={2500}>
          <RouterProvider router={router} />
        </Toaster>
      </QueryClientProvider>
    </StrictMode>,
  );
}

// Primero se resuelve la sesión de Keycloak y luego se pinta la app
initKeycloak().catch(console.error).finally(render);
