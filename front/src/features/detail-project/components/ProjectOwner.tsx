// Icono
import {CircleUserRound} from "lucide-react"

// parametro
interface params{
    name: string | undefined
}
// componente
export function ProjectOwner({name}: params) {
  return (
    <div className="flex flex-row gap-5 justify-end">
      <h3 className="flex gap-2 text-xl items-center">
        {name}
        <CircleUserRound />
      </h3>
    </div>
  );
}
