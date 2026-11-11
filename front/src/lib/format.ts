// Formateo de fechas ISO del back para mostrar en la UI
const dateFormatter = new Intl.DateTimeFormat("es-ES", {
  day: "2-digit",
  month: "short",
  year: "numeric",
});

const dateTimeFormatter = new Intl.DateTimeFormat("es-ES", {
  day: "2-digit",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

export function formatDate(iso: string | null | undefined, fallback = "—") {
  return iso ? dateFormatter.format(new Date(iso)) : fallback;
}

export function formatDateTime(iso: string | null | undefined, fallback = "—") {
  return iso ? dateTimeFormatter.format(new Date(iso)) : fallback;
}

const shortDateFormatter = new Intl.DateTimeFormat("es-ES", { day: "numeric", month: "short" });
const DAY_MS = 86_400_000;

// "Hoy", "Ayer", "Hace 3 días" o "19 oct"
export function formatRelative(iso: string | null | undefined, fallback = "—") {
  if (!iso) return fallback;
  const date = new Date(iso);
  const startOf = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
  const days = Math.round((startOf(new Date()) - startOf(date)) / DAY_MS);
  if (days === 0) return "Hoy";
  if (days === 1) return "Ayer";
  if (days > 1 && days < 7) return `Hace ${days} días`;
  return shortDateFormatter.format(date).replace(".", "");
}

// "Elena Torres" -> "ET"
export function initials(name: string | null | undefined) {
  return (name ?? "")
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0])
    .join("")
    .toUpperCase();
}
