import {
  // Sidebar
  Sidebar,
  SidebarContent,
  SidebarMenuItem,
  SidebarHeader,
  SidebarFooter,
  SidebarProvider,
  SidebarInset,
  SidebarTrigger,

} from "../../../components/index.comp";
import { DashboardFeat } from "@/features/index.featues";

export async function AppLayout() {
  
  
  return (
    <SidebarProvider>
      <Sidebar>
        <SidebarHeader>
          <SidebarMenuItem>
            <h1>TaskFlow</h1>
          </SidebarMenuItem>
        </SidebarHeader>
        <SidebarContent className="p-2">
          <SidebarMenuItem>DashBoard</SidebarMenuItem>
          <SidebarMenuItem>Mis proyectos</SidebarMenuItem>
          <SidebarMenuItem>Mis Tareas</SidebarMenuItem>
          <SidebarMenuItem>Projectos</SidebarMenuItem>
        </SidebarContent>
        <SidebarFooter>
          <SidebarMenuItem>profile</SidebarMenuItem>
        </SidebarFooter>
      </Sidebar>
      <SidebarInset>
        <DashboardFeat/>
      </SidebarInset>
      <SidebarTrigger />
    </SidebarProvider>
  );
}
