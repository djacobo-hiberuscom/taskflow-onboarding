import {
  FolderKanban,
  LayoutDashboard,
  ListChecks,
  type LucideIcon,
} from "lucide-react";

export type NavItem = { id: string; label: string; icon: LucideIcon; to: string };

// Tabs del sidebar
export const personalNav: NavItem[] = [
  { id: "dashboard", label: "Panel", icon: LayoutDashboard, to: "/" },
  { id: "my-projects", label: "Proyectos", icon: FolderKanban, to: "/projects" },
  { id: "my-tasks", label: "Tareas", icon: ListChecks, to: "/tasks" },

];

// Un tab está activo en su ruta y en sus subrutas (/projects/123 -> Proyectos)
export function isNavItemActive(item: NavItem, pathname: string) {
  if (item.to === "/") return pathname === "/";
  return pathname === item.to || pathname.startsWith(`${item.to}/`);
}

export function getActiveNavItem(pathname: string) {
  return personalNav.find((item) => isNavItemActive(item, pathname));
}
