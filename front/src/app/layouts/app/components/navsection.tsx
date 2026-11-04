import {
  SidebarMenu,
  SidebarMenuItem,
  SidebarMenuButton,
} from "@/components/index.comp";
import { type NavItem } from "./index.comp";
import { useLocation, useNavigate } from "@tanstack/react-router";

export function NavSection({ items }: { items: NavItem[] }) {
  const { pathname } = useLocation();
  const navigate = useNavigate();
  return (
    <SidebarMenu className="gap-0.5">
      {items.map(({ id, label, icon: Icon, to }) => {
        const isActive = pathname === to;
        return (
          <SidebarMenuItem key={id}>
            <SidebarMenuButton
              isActive={isActive}
              onClick={() => {
                navigate({ to });
              }}
              className={[
                "relative h-9 gap-3 rounded-lg px-3 text-[13.5px] font-normal text-foreground transition-colors",
                "hover:brightness-95",
                "data-active:bg-white/9 data-active:font-medium data-active:text-foreground",
                "focus-visible:ring-2 focus-visible:ring-[#6F8BFF]",
                // indicador lateral del elemento activo
                "before:absolute before:-left-2 before:top-2 before:bottom-2 before:w-0.75 before:rounded-full before:bg-[#6F8BFF] before:opacity-0 before:transition-opacity",
                "data-active:before:opacity-100",
              ].join(" ")}
            >
              <Icon
                className={isActive ? "text-foreground" : "text-foreground"}
                strokeWidth={1.75}
              />
              <span>{label}</span>
            </SidebarMenuButton>
          </SidebarMenuItem>
        );
      })}
    </SidebarMenu>
  );
}
