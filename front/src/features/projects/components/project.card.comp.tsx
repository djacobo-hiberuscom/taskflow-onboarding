import { Heart, Users, Pen, Archive, Trash } from "lucide-react";
import { Badge } from "#components/ui/badge";
import { Separator } from "#components/ui/separator";
import { useState } from "react";

// Contex menu
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuGroup,
  ContextMenuItem,
  ContextMenuTrigger,
} from "@/components/index.comp";

interface parameters {
  title: string;
  description: string;
  miembros: number;
  tareasAbiertas: number;
  favourite: boolean;
}

export function ProjectCard({
  title,
  description,
  miembros,
  tareasAbiertas,
  favourite,
}: parameters) {
  const [isFavourite, setIsFavourite] = useState(favourite);

  return (
    <ContextMenu>
      <ContextMenuTrigger className="flex flex-col gap-4 p-4 border rounded-xl bg-background max-w-xs hover:brightness-97">
        <div className="flex justify-between">
          <h1 className="font-bold text-xl">{title}</h1>
          <button
            className="self-start shrink-0 bg-background rounded-full p-1 hover:brightness-95 hover:cursor-pointer"
            onClick={() => setIsFavourite(!isFavourite)}
          >
            <Heart
              className={isFavourite ? "fill-red-400 text-red-400" : "text-foreground"}
            />
          </button>
        </div>

        <p className="flex-1 line-clamp-3">{description}</p>
        <Separator />
        <div className="flex gap-3 text justify-between items-center">
          <div className="flex gap-2 justify-center items-center">
            <Users />
            <Badge>{miembros} miembros</Badge>
          </div>
          <Badge className="bg-chart-5">{tareasAbiertas} tareas</Badge>
        </div>
      </ContextMenuTrigger>
      <ContextMenuContent>
        <ContextMenuGroup className="font-bold">
          <ContextMenuItem>
            <Pen />
            Edit
          </ContextMenuItem>
          <ContextMenuItem>
            <Archive />
            Archivar
          </ContextMenuItem>
          <ContextMenuItem disabled className="text-red-600">
            <Trash />
            Elimiar
          </ContextMenuItem>
        </ContextMenuGroup>
      </ContextMenuContent>
    </ContextMenu>
  );
}
