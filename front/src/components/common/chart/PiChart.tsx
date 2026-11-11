// COMPONENTES
import { Label, Pie, PieChart } from "recharts";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";

interface ChartPieProps {
  todo: number;
  inProgress: number;
  done: number;
}

const chartConfig = {
  tareas: { label: "Tareas" },
  todo: { label: "Pendiente", color: "var(--chart-1)" },
  inProgress: { label: "En curso", color: "var(--chart-2)" },
  done: { label: "Completada", color: "var(--chart-3)" },
} satisfies ChartConfig

export function ChartPieDonutText({todo, inProgress, done} : ChartPieProps) {
  const chartData = [
    { estado: "todo", tareas: todo, fill: "var(--color-todo)" },
    { estado: "inProgress", tareas: inProgress, fill: "var(--color-inProgress)"},
    { estado: "done", tareas: done, fill: "var(--color-done)" },
  ];

  const total = todo + inProgress + done;
  return (
    <Card className="flex flex-col">
      <CardHeader className="items-center pb-0">
        <CardTitle>Estado de las tareas</CardTitle>
      </CardHeader>
      <CardContent className="flex-1 pb-0">
        <ChartContainer
          config={chartConfig}
          className="mx-auto aspect-square max-h-62.5"
        >
          <PieChart>
            <ChartTooltip
              cursor={false}
              content={<ChartTooltipContent hideLabel />}
            />
            <Pie
              data={chartData}
              dataKey="tareas"
              nameKey="estado"
              innerRadius={60}
              strokeWidth={5}
            >
              <Label
                content={({ viewBox }) => {
                  if (viewBox && "cx" in viewBox && "cy" in viewBox) {
                    return (
                      <text
                        x={viewBox.cx}
                        y={viewBox.cy}
                        textAnchor="middle"
                        dominantBaseline="middle"
                      >
                        <tspan
                          x={viewBox.cx}
                          y={viewBox.cy}
                          className="fill-foreground text-3xl font-bold"
                        >
                          {total.toLocaleString()}
                        </tspan>
                        <tspan
                          x={viewBox.cx}
                          y={(viewBox.cy || 0) + 24}
                          className="fill-muted-foreground"
                        >
                          Tareas
                        </tspan>
                      </text>
                    );
                  }
                }}
              />
            </Pie>
          </PieChart>
        </ChartContainer>
      </CardContent>
    </Card>
  );
}
