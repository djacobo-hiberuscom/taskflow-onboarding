// componentes
import {
  Button,
  Badge,
  Avatar,
  AvatarFallback,
} from "@/components/index.comp";
// iconos
import { Pencil, Mail, LogOut } from "lucide-react";

// auth
import { useAuth } from "@/hooks/use-auth";

export function Header() {
  const { user, roles, logout } = useAuth();

  const nombre = (user?.given_name as string) ?? "Sin nombre";
  const email = (user?.email as string) ?? "Sin Email";
  const iniciales = nombre.charAt(0).concat(nombre.charAt(1));

  return (
    <div>

      <div className="relative h-32 overflow-hidden bg-primary/10">
        <div className="absolute -top-16 right-16 size-60 rounded-full border border-primary/20"></div>
        <div className="absolute top-8 right-40 size-36 rounded-full border border-primary/20"></div>
      </div>

      <div className="flex flex-col gap-4 px-8 pb-5 md:flex-row md:items-end">
        
        <Avatar className="-mt-14 size-28 shrink-0 ring-4 ring-background">
          <AvatarFallback className="bg-primary/10 text-3xl font-semibold text-primary">
            {iniciales}
          </AvatarFallback>
        </Avatar>

        
        <div className="flex flex-1 flex-col gap-3 md:flex-row md:items-end md:justify-between">
          <div className="flex flex-col gap-1.5">
            <h2 className="text-xl font-semibold">{nombre}</h2>
            <div className="flex flex-wrap items-center gap-3">
              <Badge>{roles}</Badge>
              <span className="flex items-center gap-1.5 text-sm text-muted-foreground">
                <Mail className="size-4" />
                {email}
              </span>
            </div>
          </div>

          <div className="flex gap-2">
            <Button variant="outline" size="sm">
              <Pencil /> Editar perfil
            </Button>
            <Button
              variant="outline"
              size="sm"
              className="border-destructive/30 text-destructive hover:bg-destructive/5 hover:text-destructive"
              onClick={() => logout()}
            >
              <LogOut /> Cerrar sesión
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
