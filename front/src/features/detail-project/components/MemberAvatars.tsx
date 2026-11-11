import { Avatar, AvatarFallback, AvatarGroup, AvatarGroupCount } from "@/components";
import type { UserSummary } from "@/lib/api/types";
import { initials } from "@/lib/format";
import { cn } from "@/lib/utils";

// color del circulo de las iniciales
const AVATAR_COLORS = [
  "bg-tone-5 text-tone-5-foreground",
  "bg-tone-3 text-tone-3-foreground",
  "bg-tone-6 text-tone-6-foreground",
  "bg-tone-4 text-tone-4-foreground",
  "bg-tone-1 text-tone-1-foreground",
];

function colorFor(id: string) {
  let acumulao = 0;
  for (const ch of id) acumulao = (acumulao * 31 + ch.charCodeAt(0)) | 0;
  return AVATAR_COLORS[Math.abs(acumulao) % AVATAR_COLORS.length];
}

export function UserAvatar({ user, className }: { user: UserSummary; className?: string }) {
  return (
    <Avatar size="sm" className={className}>
      <AvatarFallback className={cn("text-[0.625rem] font-semibold", colorFor(user.id))}>
        {initials(user.displayName)}
      </AvatarFallback>
    </Avatar>
  );
}

// Avatar + nombre (celda "Asignado")
export function UserCell({ user }: { user: UserSummary | null }) {
  if (!user) return <span className="text-muted-foreground">Sin asignar</span>;
  return (
    <span className="flex items-center gap-2">
      <UserAvatar user={user} />
      <span className="truncate">{user.displayName}</span>
    </span>
  );
}

// Pila de avatares + "N miembros"
export function MemberAvatars({ miembros, max = 4 }: { miembros: UserSummary[]; max?: number }) {
  const visibles = miembros.slice(0, max);
  const resto = miembros.length - visibles.length;

  return (
    <span className="flex items-center gap-2">
      {miembros.length > 0 && (
        <AvatarGroup>
          {visibles.map((m) => (
            <UserAvatar key={m.id} user={m} />
          ))}
          {resto > 0 && <AvatarGroupCount className="size-6 text-[0.625rem]">+{resto}</AvatarGroupCount>}
        </AvatarGroup>
      )}
      {miembros.length} {miembros.length === 1 ? "miembro" : "miembros"}
    </span>
  );
}
