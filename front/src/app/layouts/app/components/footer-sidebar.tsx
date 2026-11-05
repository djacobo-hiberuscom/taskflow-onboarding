// Iconos
import { ChevronsUpDown } from "lucide-react";
// Router
import { Link } from "@tanstack/react-router";
// Compoenentes
import { Avatar, AvatarImage } from "@/components/index.comp";

// Compoenente
export function FooterSidebar() {
  return (
    <Link
      to="/me"
      className="flex justify-start items-center h-12 gap-3 rounded-xl px-2 text-black ring-1 ring-white/10 transition-colors hover:bg-black/5 active:bg-black/10 cursor-pointer"
    >
      <Avatar className="p-1">
        <AvatarImage src="https://www.svgrepo.com/show/452030/avatar-default.svg" />
      </Avatar>
      <div className="flex min-w-0 flex-1 flex-col text-left leading-tight">
        <span className="truncate text-[13px] font-medium">Mi perfil</span>
        <span className="truncate text-xs text-black">Cuenta y sesión</span>
      </div>
      <ChevronsUpDown className="text-black" strokeWidth={1.75} />
    </Link>
  );
}
