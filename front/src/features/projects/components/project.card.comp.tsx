import { Heart, Users } from "lucide-react";
import { Badge } from "#components/ui/badge";
import { Separator } from "#components/ui/separator";
import { useState } from "react";

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
    <div className="flex flex-col gap-4 p-4 border rounded-xl bg-background max-w-xs">
      <div className="flex justify-between">
        <h1 className="font-bold text-xl">{title}</h1>
        <div className="bg-background rounded-full p-1 hover:brightness-95 hover:cursor-pointer" onClick={() => setIsFavourite(!isFavourite)}>
            <Heart className={isFavourite ? "fill-red-400" : "text-foreground"}/>
        </div>
      </div>

      <p>{description}</p>
      <Separator />
      <div className="flex gap-3 text justify-between items-center">
        <div className="flex gap-2 justify-center items-center">
          
          <Users />
          <Badge>{miembros} miembros</Badge>
        </div>
        <Badge className="bg-chart-5">{tareasAbiertas} tareas</Badge>
      </div>
    </div>
  );
}
