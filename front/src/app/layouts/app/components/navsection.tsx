import { Link, useLocation } from "@tanstack/react-router";
import { SidebarMenu, SidebarMenuButton, SidebarMenuItem } from "@/components/index.comp";
import { cn } from "@/lib/utils";
import { isNavItemActive, type NavItem } from "./personalNav";

// Lista de tabs del sidebar
export function NavSection({ items }: { items: NavItem[] }) {
  const { pathname } = useLocation();

  return (
    <SidebarMenu className="gap-0.5">
      {items.map((item) => {
        const { id, label, icon: Icon, to } = item;
        return (
          <SidebarMenuItem key={id}>
            <SidebarMenuButton
              isActive={isNavItemActive(item, pathname)}
              render={<Link to={to} />}
              className={cn(
                "relative h-9 gap-3 rounded-lg px-3 text-[13.5px] font-normal text-foreground transition-colors",
                "hover:brightness-95",
                "data-active:bg-white/9 data-active:font-medium data-active:text-foreground",
                "focus-visible:ring-2 focus-visible:ring-[#6F8BFF]",
                // indicador lateral del elemento activo
                "before:absolute before:-left-2 before:top-2 before:bottom-2 before:w-0.75 before:rounded-full before:bg-[#6F8BFF] before:opacity-0 before:transition-opacity",
                "data-active:before:opacity-100",
              )}
            >
              <Icon className="text-foreground" strokeWidth={1.75} />
              <span>{label}</span>
            </SidebarMenuButton>
          </SidebarMenuItem>
        );
      })}
    </SidebarMenu>
  );
}
