import { useState } from "react";
import {
  // Sidebar
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarTrigger,
  Separator,

  // Avatar
} from "../../../components/index.comp";
import {
  FooterSidebar,
  HeaderSiderBar,
  StickyHeader,
  personalNav,
  type NavItem,
} from "./components/index.comp";
import { DashboardFeat } from "@/features/index.featues";



function NavSection({
  items,
  active,
  onSelect,
}: {
  items: NavItem[];
  active: string;
  onSelect: (id: string) => void;
}) {
  return (
    <SidebarMenu className="gap-0.5">
      {items.map(({ id, label, icon: Icon }) => {
        const isActive = id === active;
        return (
          <SidebarMenuItem key={id}>
            <SidebarMenuButton
              isActive={isActive}
              onClick={() => onSelect(id)}
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

export function AppLayout() {
  const [active, setActive] = useState("dashboard");

  return (
    <SidebarProvider>
      <Sidebar>
        <div className="flex size-full flex-col bg-background text-foreground">
          <SidebarHeader className="px-4 pt-5 pb-4">
            <HeaderSiderBar />
          </SidebarHeader>

          <SidebarContent className="gap-0 px-4 py-2">
            <NavSection
              items={personalNav}
              active={active}
              onSelect={setActive}
            />
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

      <SidebarInset className="min-w-0 bg-[#F3F5F9]">
        <StickyHeader title="Panel"/>
        

        <div className="mx-auto w-full max-w-350 flex-1">
          <DashboardFeat />
        </div>
      </SidebarInset>
    </SidebarProvider>
  );
}
