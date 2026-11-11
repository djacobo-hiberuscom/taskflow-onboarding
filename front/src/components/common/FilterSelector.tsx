import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { cn } from "@/lib/utils";

export interface SelectOption<T extends string = string> {
  label: string;
  value: T;
}

interface FilterSelectorProps<T extends string> {
  items: SelectOption<T>[];
  placeholder: string;
  value?: T | null;
  onChange?: (value: T | null) => void;
  className?: string;
  disabled?: boolean;
  // Prefijo dentro del trigger, p. ej. "Estado" -> "Estado  Todos"
  label?: string;
}

// Select de shadcn simplificado (tipado según los valores de las opciones)
export function FilterSelector<T extends string>({
  items,
  placeholder,
  value,
  onChange,
  className,
  disabled = false,
  label,
}: FilterSelectorProps<T>) {
  return (
    <Select
      disabled={disabled}
      items={items}
      value={value ?? null}
      onValueChange={(v) => onChange?.((v as T | null) ?? null)}
    >
      <SelectTrigger className={cn("w-45", className)}>
        {label && <span className="text-muted-foreground">{label}</span>}
        <SelectValue placeholder={placeholder} className="font-medium" />
      </SelectTrigger>
      <SelectContent className="max-h-100 w-fit min-w-(--anchor-width) max-w-(--available-width)">
        <SelectGroup>
          {items.map((item) => (
            <SelectItem key={item.value} value={item.value}>
              {item.label}
            </SelectItem>
          ))}
        </SelectGroup>
      </SelectContent>
    </Select>
  );
}
