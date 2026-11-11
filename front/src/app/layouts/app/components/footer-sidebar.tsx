// Iconos
import { ChevronsUpDown } from "lucide-react";
// Router
import { Link } from "@tanstack/react-router";
// Compoenentes
import { Avatar, AvatarFallback } from "@/components/index";
// hooks
import { useMe } from "@/hooks/use-users";
import { initials } from "#lib/format";
// Compoenente
export function FooterSidebar() {
  const { data: me } = useMe();
  const nombre = me?.displayName ?? "Cargando..."
  return (
    <Link
      to="/me"
      className="flex justify-start items-center h-12 gap-3 rounded-xl px-2 text-sidebar-foreground transition-colors hover:bg-sidebar-accent active:bg-sidebar-accent/80 cursor-pointer"
    >
      <Avatar className="size-8 shrink-0">
        <AvatarFallback className="bg-primary text-xs font-semibold text-primary-foreground">
          {initials(me?.displayName)}
        </AvatarFallback>
      </Avatar>
      <div className="flex min-w-0 flex-1 flex-col text-left leading-tight">
        <span className="truncate text-[13px] font-medium">{nombre}</span>
        <span className="truncate text-xs text-muted-foreground">
          {me?.email ?? "Mi perfil"}
        </span>
      </div>
      <ChevronsUpDown className="text-muted-foreground" strokeWidth={1.75} />
    </Link>
  );
}
