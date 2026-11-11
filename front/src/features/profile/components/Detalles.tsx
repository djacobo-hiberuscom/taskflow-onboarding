import { useAuth } from "@/hooks/use-auth";

// Datos personales
export function DetallesMe() {
  const { user, roles } = useAuth();
  const rows = [
    {label: "Nombre", value: user?.given_name},
    {label: "Correo electronico", value: user?.email},
    {label: "Role en la app", value: roles[0]}
  ];

  return (
    <dl className="mx-8 mb-4 divide-y border-t">
      {rows.map(({ label, value }) => (
        <div key={label} className="grid grid-cols-[180px_1fr] gap-4 py-3 text-sm">
          <dt className="text-muted-foreground">{label}</dt>
          <dd className="font-medium">{(value as string) || "—"}</dd>
        </div>
      ))}
    </dl>
  );
}
