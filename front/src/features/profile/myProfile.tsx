import { DetallesMe, LastChanges, MeCard, StatsMe } from "./components";

// FEAT: Mi perfil
export function MyProfileFeat() {
  return (
    <div className="flex flex-col gap-5 p-5">
      <MeCard />
      <StatsMe />
    </div>
  );
}
