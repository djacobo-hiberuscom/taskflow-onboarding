// componentes
import { useMemo, useState } from "react";
import { Area, AreaChart, CartesianGrid, XAxis } from "recharts";
import {
  eachDayOfInterval,
  endOfDay,
  format,
  parseISO,
  startOfDay,
  subDays,
} from "date-fns";
import { es } from "date-fns/locale";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  ChartContainer,
  ChartLegend,
  ChartLegendContent,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { Task } from "@/lib/api/types";

interface ChartAreaParams {
  tasks: Task[];
}

const chartConfig = {
  pendientes: { label: "Pendientes", color: "var(--chart-1)" },
  completadas: { label: "Completadas", color: "var(--chart-2)" },
} satisfies ChartConfig;

const RANGES = [
  { value: "7", label: "Últimos 7 días" },
  { value: "30", label: "Últimos 30 días" },
  { value: "90", label: "Últimos 3 meses" },
];

type Point = { date: string; pendientes: number; completadas: number };

// Para cada día del rango: cuántas tareas había pendientes y cuántas completadas al final de ese día
function buildTimeline(tasks: Task[], days: number): Point[] {
  const today = startOfDay(new Date());
  return eachDayOfInterval({ start: subDays(today, days - 1), end: today }).map(
    (day) => {
      const limit = endOfDay(day).getTime();
      let pendientes = 0;
      let completadas = 0;
      for (const t of tasks) {
        if (new Date(t.createdAt).getTime() > limit) continue; // aún no existía
        // Sin completedAt en el back: aproximamos con updatedAt de las tareas Done
        const doneAt =
          t.status === "Done" ? new Date(t.updatedAt).getTime() : Infinity;
        if (doneAt <= limit) completadas++;
        else pendientes++;
      }
      return { date: format(day, "yyyy-MM-dd"), pendientes, completadas };
    },
  );
}

const shortDate = (value: string) =>
  format(parseISO(value), "d MMM", { locale: es });

export function ChartAreaTasks({ tasks }: ChartAreaParams) {
  const [range, setRange] = useState("7");

  const data = useMemo(
    () => buildTimeline(tasks, Number(range)),
    [tasks, range],
  );
  const rangeLabel = RANGES.find((r) => r.value === range)?.label.toLowerCase();

  return (
    <Card className="pt-0">
      <CardHeader className="flex items-center gap-2 space-y-0 border-b py-5 sm:flex-row">
        <div className="grid flex-1 gap-1">
          <CardTitle>Evolución de las tareas</CardTitle>
          <CardDescription>
            Pendientes y completadas en los {rangeLabel}
          </CardDescription>
        </div>
        <Select
          items={RANGES}
          value={range}
          onValueChange={(v) => v && setRange(v as string)}
        >
          <SelectTrigger
            className="hidden w-40 rounded-lg sm:ml-auto sm:flex"
            aria-label="Rango de fechas"
          >
            <SelectValue />
          </SelectTrigger>
          <SelectContent className="rounded-xl">
            {RANGES.map((r) => (
              <SelectItem key={r.value} value={r.value} className="rounded-lg">
                {r.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </CardHeader>

      <CardContent className="px-2 pt-4 sm:px-6 sm:pt-6">
        <ChartContainer
          config={chartConfig}
          className="aspect-auto h-62.5 w-full"
        >
          <AreaChart data={data}>
            <defs>
              <linearGradient id="fillPendientes" x1="0" y1="0" x2="0" y2="1">
                <stop
                  offset="5%"
                  stopColor="var(--color-pendientes)"
                  stopOpacity={0.8}
                />
                <stop
                  offset="95%"
                  stopColor="var(--color-pendientes)"
                  stopOpacity={0.1}
                />
              </linearGradient>
              <linearGradient id="fillCompletadas" x1="0" y1="0" x2="0" y2="1">
                <stop
                  offset="5%"
                  stopColor="var(--color-completadas)"
                  stopOpacity={0.8}
                />
                <stop
                  offset="95%"
                  stopColor="var(--color-completadas)"
                  stopOpacity={0.1}
                />
              </linearGradient>
            </defs>
            <CartesianGrid vertical={false} />
            <XAxis
              dataKey="date"
              tickLine={false}
              axisLine={false}
              tickMargin={8}
              minTickGap={32}
              tickFormatter={shortDate}
            />
            <ChartTooltip
              cursor={false}
              content={
                <ChartTooltipContent
                  labelFormatter={(v) => shortDate(String(v))}
                  indicator="dot"
                />
              }
            />
            <Area
              dataKey="completadas"
              type="monotone"
              fill="url(#fillCompletadas)"
              stroke="var(--color-completadas)"
              stackId="a"
            />
            <Area
              dataKey="pendientes"
              type="monotone"
              fill="url(#fillPendientes)"
              stroke="var(--color-pendientes)"
              stackId="a"
            />
            <ChartLegend content={<ChartLegendContent />} />
          </AreaChart>
        </ChartContainer>
      </CardContent>
    </Card>
  );
}
