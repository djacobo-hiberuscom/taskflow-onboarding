import { Separator, SidebarTrigger,  } from "@/components";
import {DarkToggle} from "./index"
// Fecha de hoy ("lunes, 10 de noviembre")
const today = new Intl.DateTimeFormat("es-ES", {
  weekday: "long",
  day: "numeric",
  month: "long",
  timeZone: "Europe/Madrid",
}).format(new Date());

// Header superior del layout
export function StickyHeader({ title }: { title: string }) {
  return (
    <header className="sticky top-0 z-10 flex h-14 shrink-0 items-center gap-3 border-b border-border bg-sidebar px-4 backdrop-blur-md">
      <SidebarTrigger className="-ml-1 text-muted-foreground hover:bg-muted" />
      <Separator orientation="vertical" className="h-5 bg-border-strong" />
      <h2 className="text-[15px] font-semibold tracking-[-0.01em] text-foreground">{title}</h2>
      <time className="ml-auto text-[13px] text-muted-foreground first-letter:uppercase">{today}</time>
      <DarkToggle />
    </header>
  );
}
