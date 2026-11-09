import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import { RouterProvider } from "@tanstack/react-router";
import {
  QueryClientProvider,
  QueryClient,
  QueryCache,
} from "@tanstack/react-query";
import { router } from "./app/router.tsx";
import { initKeycloak } from "@/lib/auth/keycloak.ts";
import { ApiError } from "#lib/api/client";
import { Toaster } from "#components/ui/toast";

const queryClient = new QueryClient({
  queryCache: new QueryCache({
    onError: (error) => {
      if (!(error instanceof ApiError)) return;
      if (error.status === 401) router.navigate({ to: "/timeout" });
      if (error.status === 403) router.navigate({ to: "/403" });
    },
  }),
  defaultOptions: {
    queries: {
      retry: (count, error) =>
        error instanceof ApiError && error.status < 500 ? false : count < 2,
    },
  },
});

function render() {
  createRoot(document.getElementById("root")!).render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <Toaster>
          <RouterProvider router={router} />
        </Toaster>
      </QueryClientProvider>
    </StrictMode>,
  );
}

initKeycloak().catch(console.error).finally(render);
