import { useQuery } from "@tanstack/react-query";
import { usersApi } from "@/lib/api/endpoints";
import { userKeys } from "@/lib/query-keys";

// GET /me (usuario local del back, con su id interno)
export function useMe() {
  return useQuery({ queryKey: userKeys.me(), queryFn: usersApi.me });
}

// GET /users/assignable
export function useAssignableUsers() {
  return useQuery({
    queryKey: userKeys.assignable(),
    queryFn: usersApi.assignable,
    staleTime: 5 * 60 * 1000,
  });
}
