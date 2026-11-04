import { Button } from "#components/ui/button";
import { Plus } from "lucide-react";

interface parameters {
  onClic: () => void;
}

export function AddButton({ onClic }: parameters) {
  return (
    <Button
      className="rounded-full min-w-12 min-h-12 bg-primary hover:cursor-pointer hover:bg-primary hover:brightness-120"
      variant="outline"
      onClick={onClic}
    >
      <Plus className="size-7 text-white" />
    </Button>
  );
}
