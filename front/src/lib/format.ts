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
