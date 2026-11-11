import { useAuth } from "@/hooks/use-auth";

// Datos personales
export function DetallesMe() {
  const { user, roles } = useAuth();
  const rows = [
    { label: "Nombre completo", value: user?.name },
    { label: "Correo electrónico", value: user?.email },
    { label: "Rol", value: roles.join(", ") },
  ];

  return (
    <div className="bg-background p-5 flex flex-col gap-3 border rounded-xl">
      <h1>Detalles</h1>
      {rows.map(({ label, value }) => (
        <div key={label} className="flex gap-2">
          <h2 className="flex flex-1">{label}</h2>
          <span>{value ?? "—"}</span>
        </div>
      ))}
    </div>
  );
}
