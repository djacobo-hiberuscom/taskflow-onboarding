import { useState } from "react";
import {
  ChevronsUpDown,
  FolderKanban,
  Folders,
  LayoutDashboard,
  ListChecks,
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
} from "../../../components/index.comp";
import { DashboardFeat } from "@/features/index.featues";

type NavItem = { id: string; label: string; icon: LucideIcon };

const personalNav: NavItem[] = [
  { id: "dashboard", label: "Panel", icon: LayoutDashboard },
  { id: "my-projects", label: "Mis proyectos", icon: FolderKanban },
  { id: "my-tasks", label: "Mis tareas", icon: ListChecks },
];

const globalNav: NavItem[] = [
  { id: "projects", label: "Proyectos", icon: Folders },
];

const today = new Intl.DateTimeFormat("es-ES", {
  weekday: "long",
  day: "numeric",
  month: "long",
  timeZone: "Europe/Madrid",
}).format(new Date());


function BrandMark() {
  return (
    <span
      aria-hidden
      className="flex size-8 shrink-0 flex-col justify-center gap-[3px] rounded-lg bg-white/[0.07] px-1.5 ring-1 ring-white/10"
    >
      <span className="h-[3px] w-1/3 rounded-full bg-[#6F8BFF]/50" />
      <span className="h-[3px] w-2/3 rounded-full bg-[#6F8BFF]/80" />
      <span className="h-[3px] w-full rounded-full bg-[#6F8BFF]" />
    </span>
  );
}

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
                "relative h-9 gap-3 rounded-lg px-3 text-[13.5px] font-normal text-[#A7B2C6] transition-colors",
                "hover:bg-white/[0.06] hover:text-white",
                "data-active:bg-white/[0.09] data-active:font-medium data-active:text-white",
                "focus-visible:ring-2 focus-visible:ring-[#6F8BFF]",
                // indicador lateral del elemento activo
                "before:absolute before:-left-2 before:top-2 before:bottom-2 before:w-[3px] before:rounded-full before:bg-[#6F8BFF] before:opacity-0 before:transition-opacity",
                "data-active:before:opacity-100",
              ].join(" ")}
            >
              <Icon
                className={
                  isActive ? "text-[#8FA5FF]" : "text-[#6E7B93]"
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
  const current = [...personalNav, ...globalNav].find((i) => i.id === active);

  return (
    <SidebarProvider>
      <Sidebar>
        <div className="flex size-full flex-col bg-[#101826] text-[#E8ECF4]">
          <SidebarHeader className="px-4 pt-5 pb-4">
            <div className="flex items-center gap-3 px-1">
              <BrandMark />
              <span className="text-[17px] font-semibold tracking-[-0.02em] text-white">
                TaskFlow
              </span>
            </div>
          </SidebarHeader>

          <SidebarContent className="gap-0 px-4 py-2">
            <NavSection items={personalNav} active={active} onSelect={setActive} />
            <Separator className="my-4 bg-white/10" />
            <NavSection items={globalNav} active={active} onSelect={setActive} />
          </SidebarContent>

          <SidebarFooter className="p-3">
            <SidebarMenu>
              <SidebarMenuItem>
                <SidebarMenuButton
                  size="lg"
                  className="h-12 gap-3 rounded-xl px-2 text-[#E8ECF4] ring-1 ring-white/10 hover:bg-white/[0.06] hover:text-white focus-visible:ring-[#6F8BFF]"
                >
                  <span
                    aria-hidden
                    className="flex size-8 shrink-0 items-center justify-center rounded-full bg-[#6F8BFF] text-xs font-semibold text-[#101826]"
                  >
                    TU
                  </span>
                  <span className="flex min-w-0 flex-1 flex-col text-left leading-tight">
                    <span className="truncate text-[13px] font-medium">Mi perfil</span>
                    <span className="truncate text-xs text-[#8793AA]">Cuenta y sesión</span>
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

        <div className="mx-auto w-full max-w-[1400px] flex-1">
          <DashboardFeat />
        </div>
      </SidebarInset>
    </SidebarProvider>
  );
}
