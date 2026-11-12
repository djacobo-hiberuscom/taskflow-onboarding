import { Outlet, useLocation } from "@tanstack/react-router";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuItem,
  SidebarProvider,
  BackGroundPatern,
} from "@/components";
import {
  FooterSidebar,
  getActiveNavItem,
  HeaderSiderBar,
  NavSection,
  personalNav,
  StickyHeader,
} from "./components";

// Layout principal: sidebar + header fijos, la feat activa se pinta en <Outlet />
export function AppLayout() {
  const { pathname } = useLocation();
  const title = getActiveNavItem(pathname)?.label ?? "";

  return (
    <SidebarProvider>
      <Sidebar>
        <div className="flex size-full flex-col bg-sidebar text-sidebar-foreground">
          <SidebarHeader className="px-4 pt-5 pb-4">
            <HeaderSiderBar />
          </SidebarHeader>

          <SidebarContent className="gap-0 px-4 py-2">
            <NavSection items={personalNav} />
          </SidebarContent>

          <SidebarFooter className="p-3">
            <SidebarMenu>
              <SidebarMenuItem>
                <FooterSidebar />
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarFooter>
        </div>
      </Sidebar>

      <SidebarInset className="min-w-0 bg-background">
        <StickyHeader title={title} />
        <div className="mx-auto w-full max-w-350 flex-1 flex flex-col">
          <BackGroundPatern />
          <div className="relative z-10">
            <Outlet />
          </div>
        </div>
      </SidebarInset>
    </SidebarProvider>
  );
}
