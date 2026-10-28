import { useState } from "react";
import {
  ChevronsUpDown,
  FolderKanban,
  LayoutDashboard,
  ListChecks,
  User,
  type LucideIcon,
} from "lucide-react";
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
  Avatar,
  AvatarImage
} from "../../../components/index.comp";
import { DashboardFeat } from "@/features/index.featues";

type NavItem = { id: string; label: string; icon: LucideIcon };

const personalNav: NavItem[] = [
  { id: "dashboard", label: "Panel", icon: LayoutDashboard },
  { id: "my-projects", label: "Proyectos", icon: FolderKanban },
  { id: "my-tasks", label: "Tareas", icon: ListChecks },
  {id: "my-profile", label: "Perfil", icon: User}
];

const today = new Intl.DateTimeFormat("es-ES", {
  weekday: "long",
  day: "numeric",
  month: "long",
  timeZone: "Europe/Madrid",
}).format(new Date());

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
                className={
                  isActive ? "text-foreground" : "text-foreground"
                }
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
  const current = [...personalNav].find((i) => i.id === active);

  return (
    <SidebarProvider>
      <Sidebar>
        <div className="flex size-full flex-col bg-background text-foreground">
          <SidebarHeader className="px-4 pt-5 pb-4">
            <div className="flex items-center gap-3 px-1">
              
              <span className="text-[17px] font-semibold tracking-[-0.02em] text-foreground">
                TaskFlow
              </span>
            </div>
          </SidebarHeader>

          <SidebarContent className="gap-0 px-4 py-2">
            <NavSection items={personalNav} active={active} onSelect={setActive} />
            <Separator className="my-4 bg-white/10" />
            
          </SidebarContent>

          <SidebarFooter className="p-3">
            <SidebarMenu>
              <SidebarMenuItem>
                <SidebarMenuButton
                  size="lg"
                  className="h-12 gap-3 rounded-xl px-2 text-black ring-1 ring-white/10 hover:bg-white/6 hover: focus-visible:ring-[#6F8BFF]"
                >
                  <Avatar className="p-1">
                    <AvatarImage src="https://www.svgrepo.com/show/452030/avatar-default.svg"/>
                  </Avatar>
                  <span className="flex min-w-0 flex-1 flex-col text-left leading-tight">
                    <span className="truncate text-[13px] font-medium">Mi perfil</span>
                    <span className="truncate text-xs text-black">Cuenta y sesión</span>
                  </span>
                  <ChevronsUpDown className="text-[#6E7B93]" strokeWidth={1.75} />
                </SidebarMenuButton>
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarFooter>
        </div>
      </Sidebar>

      <SidebarInset className="min-w-0 bg-[#F3F5F9]">
        <header className="sticky top-0 z-10 flex h-14 shrink-0 items-center gap-3 border-b border-[#E3E7EF] bg-[#F3F5F9]/85 px-4 backdrop-blur-md">
          <SidebarTrigger className="-ml-1 text-[#4A5568] hover:bg-[#E6EAF2]" />
          <Separator orientation="vertical" className="h-5 bg-[#D5DAE5]" />
          <h2 className="text-[15px] font-semibold tracking-[-0.01em] text-[#101826]">
            {current?.label}
          </h2>
          <time className="ml-auto text-[13px] text-[#6B778C] first-letter:uppercase">
            {today}
          </time>
        </header>

        <div className="mx-auto w-full max-w-350 flex-1">
          <DashboardFeat />
        </div>
      </SidebarInset>
    </SidebarProvider>
  );
}
