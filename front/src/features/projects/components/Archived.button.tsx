import { Button } from "#components/ui/button";
import { Archive } from "lucide-react";

interface parameters {
  onClic: () => void;
  isAdmin: boolean
}

export function ArchivedButton({ onClic, isAdmin}: parameters) {
  return (
    <Button
      disabled={!isAdmin}
      className="rounded-full min-w-12 min-h-12 bg-background hover:cursor-pointer hover:brightness-90"
      variant="outline"
      onClick={onClic}
    >
      <Archive className="size-7" />
    </Button>
  );
}
