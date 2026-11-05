// Iconos
import {
  FolderKanban,
  LayoutDashboard,
  ListChecks,
  User,
  type LucideIcon,
} from "lucide-react";

// type
export type NavItem = { id: string; label: string; icon: LucideIcon, to: string };

// Lista de tabs del sidebar
export const personalNav: NavItem[] = [
  { id: "dashboard", label: "Panel", icon: LayoutDashboard, to: "/"},
  { id: "my-projects", label: "Proyectos", icon: FolderKanban, to: "/projects" },
  { id: "my-tasks", label: "Tareas", icon: ListChecks, to: "/tasks" },
  { id: "my-profile", label: "Perfil", icon: User , to: "/me"},
];