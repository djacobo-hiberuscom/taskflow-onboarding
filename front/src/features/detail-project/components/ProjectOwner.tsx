import { CircleUserRound } from "lucide-react";

// Creador del proyecto
export function ProjectOwner({ name }: { name?: string }) {
  return (
    <div className="flex flex-row gap-5 justify-end">
      <h3 className="flex gap-2 text-xl items-center">
        {name}
        <CircleUserRound />
      </h3>
    </div>
  );
}
