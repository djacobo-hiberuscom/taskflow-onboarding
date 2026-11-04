import type { Project } from "#lib/api/types";
import {
    DrawerClose,
  DrawerContent,
  DrawerFooter,
  DrawerHeader,
  DrawerTitle,

  Button
} from "@/components/index.comp"

interface parameters {
  project: Project | null
}

export function ProjectDrawer({project}: parameters) {
  return (
    <DrawerContent>
      <DrawerHeader>
        <DrawerTitle className="text-xl">{project?.name}</DrawerTitle>
      </DrawerHeader>
      <div className="flex-1 p-4">
        <div className="rounded-2xl bg-muted group-data-[swipe-axis=x]/drawer-popup:size-full group-data-[swipe-axis=y]/drawer-popup:h-80 group-data-[swipe-axis=y]/drawer-popup:w-full" />
      </div>
      <DrawerFooter>
        <DrawerClose render={<Button>Close</Button>} />
      </DrawerFooter>
    </DrawerContent>
  );
}
