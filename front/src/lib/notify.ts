import { toast } from "@/components/ui/toast";

// Atajos para lanzar toasts
export const notify = {
  success: (description: string) => toast.add({ type: "success", description }),
  error: (description: string) => toast.add({ type: "error", description }),
};
