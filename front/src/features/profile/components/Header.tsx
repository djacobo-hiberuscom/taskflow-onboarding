// componentes
import {
  Button,
  Badge,
  Avatar,
  Label,
  AvatarFallback,
} from "@/components/index.comp";
// iconos
import { Camera } from "lucide-react";

// auth
import { useAuth } from "@/hooks/use-auth";

export function Header() {
  const { user, roles, logout } = useAuth();

  const nombre = (user?.given_name as string) ?? "Sin nombre";
  const email = (user?.email as string) ?? "Sin Email";
  const iniciales = nombre.charAt(0).concat(nombre.charAt(1));

  return (
    <div className="">
      <div className="relative h-32 overflow-hidden bg-primary/10">
        <div className="absolute -top-16 right-16 size-60 rounded-full border border-primary/20"></div>
        <div className="absolute top-8 right-40 size-36 rounded-full border border-primary/20"></div>
      </div>

      <div className="flex flex-col gap-4 px-8 md:flex-row md:items-end">
        {/* Avatar que "sube" encima del banner */}
        <div className="relative -mt-16 w-fit">
          <Avatar className="size-32 ring-4 ring-background">
            <AvatarFallback className="bg-primary/10 text-4xl font-semibold text-primary">
              {iniciales || "?"}
            </AvatarFallback>
          </Avatar>
          <Button
            size="icon-sm"
            variant="outline"
            className="absolute right-1 bottom-1 rounded-full"
          >
            <Camera />
          </Button>
        </div>

        <div>
          <Label>{user?.sub}</Label>
          <div>
            <Badge>{roles}</Badge>
            <Label></Label>
          </div>
          <div>
            <Button>Editar perfil</Button>
            <Button variant="destructive">Cerrar sesion</Button>
          </div>
        </div>
      </div>
    </div>
  );
}
