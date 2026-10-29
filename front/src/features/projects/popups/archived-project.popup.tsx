// COMPONENTES
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  Button,
} from "@/components/index.comp";

// PROYECTOS
import type { Project } from "#lib/api/types";
import PROYECTOS from "@/assets/tempdata/projects.json";

const proyectoArchived = PROYECTOS.filter((p) => p.archived_at !== null);

export function ArchivedPopUp() {
  return (
    <Dialog>
      <DialogTrigger
        render={<Button variant="outline">Sticky Footer</Button>}
      />
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Archivados</DialogTitle>
          <DialogDescription>
            Proyectos archivados
          </DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-3 p-5 border rounded-2xl bg-background max-w-xl max-h-xl" onScroll={() => {}}>
          {proyectoArchived.map((pa) => {
              return <ArchivedRecyclerView proyecto={pa} />;
            })}
        </div>
        
        <DialogFooter>
          <DialogClose render={<Button variant="outline">Close</Button>} />
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// COMPONENTES DEL POPUP
interface recyclerParam {
  proyecto: Project;
}
function ArchivedRecyclerView({ proyecto }: recyclerParam) {
  return <div>{proyecto.name}</div>;
}
