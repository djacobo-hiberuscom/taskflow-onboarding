// COMPONENTES
import { Button } from "@/components/ui/button";
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerFooter,
  DrawerHeader,
  DrawerTitle,
} from "@/components/index.comp";

import type { Task } from "#lib/api/types";

interface parameters {
  tarea: Task | null;
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
}

export function DeailTaskDrawer({ tarea, isOpen, onOpenChange }: parameters) {
  return (
    <Drawer
      modal={false}
      disablePointerDismissal
      swipeDirection="right"
      open={isOpen}
      onOpenChange={onOpenChange}
    >
      <DrawerContent>
        <DrawerHeader>
          <DrawerTitle>{tarea?.title}</DrawerTitle>
        </DrawerHeader>
        <div className="flex-1 p-4">
          <div className="rounded-2xl bg-muted group-data-[swipe-axis=x]/drawer-popup:size-full group-data-[swipe-axis=y]/drawer-popup:h-80 group-data-[swipe-axis=y]/drawer-popup:w-full" />
        </div>
        <DrawerFooter>
          <DrawerClose render={<Button>Close</Button>} />
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  );
}
