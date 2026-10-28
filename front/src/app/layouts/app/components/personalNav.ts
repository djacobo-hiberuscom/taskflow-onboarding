import {
  FolderKanban,
  LayoutDashboard,
  ListChecks,
  User,
  type LucideIcon,
} from "lucide-react";

export type NavItem = { id: string; label: string; icon: LucideIcon };

export const personalNav: NavItem[] = [
  { id: "dashboard", label: "Panel", icon: LayoutDashboard },
  { id: "my-projects", label: "Proyectos", icon: FolderKanban },
  { id: "my-tasks", label: "Tareas", icon: ListChecks },
  { id: "my-profile", label: "Perfil", icon: User },
];