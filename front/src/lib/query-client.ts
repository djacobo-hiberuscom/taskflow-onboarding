import { QueryCache, QueryClient } from "@tanstack/react-query";
import { ApiError } from "@/lib/api/client";
import { router } from "@/app/router";

// Cliente global de TanStack Query
export const queryClient = new QueryClient({
  queryCache: new QueryCache({
    // Errores de auth globales -> pantallas de error
    onError: (error) => {
      if (!(error instanceof ApiError)) return;
      if (error.status === 401) router.navigate({ to: "/timeout" });
      if (error.status === 403) router.navigate({ to: "/403" });
    },
  }),
  defaultOptions: {
    queries: {
      // No reintentar errores 4xx
      retry: (count, error) =>
        error instanceof ApiError && error.status < 500 ? false : count < 2,
    },
  },
});
