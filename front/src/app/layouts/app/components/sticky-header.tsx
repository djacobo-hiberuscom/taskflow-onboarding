import {Separator, SidebarTrigger} from "@/components/index.comp"


interface params {
  title: string;
}

const today = new Intl.DateTimeFormat("es-ES", {
  weekday: "long",
  day: "numeric",
  month: "long",
  timeZone: "Europe/Madrid",
}).format(new Date());

export function StickyHeader({title} :params) {
  return (
    <header className="sticky top-0 z-10 flex h-14 shrink-0 items-center gap-3 border-b border-[#E3E7EF] bg-[#F3F5F9]/85 px-4 backdrop-blur-md">
      <SidebarTrigger className="-ml-1 text-[#4A5568] hover:bg-[#E6EAF2]" />
      <Separator orientation="vertical" className="h-5 bg-[#D5DAE5]" />
      <h2 className="text-[15px] font-semibold tracking-[-0.01em] text-[#101826]">
        {title}
      </h2>
      <time className="ml-auto text-[13px] text-[#6B778C] first-letter:uppercase">
        {today}
      </time>
    </header>
  );
}
