import { useState } from "react";
import { QueryState } from "@/components";
import { useProjects } from "@/hooks/use-project";
import type { Project } from "@/lib/api/types";
import {
  PopupContainer,
  Projects,
  type ProjectsPopup,
  ProjectsHeader,
  ProjectsTabs,
  ProjectsEmptyState,
  ProjectsPagination,
} from "./components";
import { useAuth } from "#hooks/use-auth";

const PAGE_SIZE = 6;

// FEAT: Proyectos
export function ProjectsFeat() {
  const { isAdmin } = useAuth();
  const [page, setPage] = useState(1);
  const [popup, setPopup] = useState<ProjectsPopup | null>(null);
  const [selected, setSelected] = useState<Project | undefined>(undefined);

  const { data, isLoading, error } = useProjects(
    { page, pageSize: PAGE_SIZE },
    { keepPrevious: true },
  );

  const { data: archived } = useProjects(
    { archived: true, pageSize: 1 },
    { enabled: isAdmin },
  );

  const total = data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  // Abre un popup sobre un proyecto concreto
  const openFor = (type: ProjectsPopup) => (p: Project) => {
    setSelected(p);
    setPopup(type);
  };

  const openNew = () => setPopup("new");
  const openArchived = () => setPopup("archived-list");

  if (isLoading || error)
    return <QueryState isLoading={isLoading} error={error} />;

  return (
    <div className="flex flex-1 flex-col gap-5 p-6">
      <PopupContainer
        popup={popup}
        proyecto={selected}
        close={() => setPopup(null)}
      />

      <ProjectsHeader
        archivedCount={archived?.totalCount}
        openArchived={openArchived}
        openNewProject={openNew}
      />

      <ProjectsTabs total={total} />

      {total === 0 ? (
        <ProjectsEmptyState onCreate={openNew} onOpenArchived={openArchived} />
      ) : (
        <>
          <Projects
            projects={data?.items ?? []}
            onEdit={openFor("edit")}
            onArchive={openFor("archive")}
            onDelete={openFor("delete")}
          />
          <ProjectsPagination
            page={page}
            totalPages={totalPages}
            total={total}
            onChange={setPage}
          />
        </>
      )}
    </div>
  );
}
