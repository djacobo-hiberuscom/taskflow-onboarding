import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {cn} from "@/lib/utils"
interface item {
  label: string,
  value: string
}

interface parameters {
  items: item[],
  placeholder: string,
  value?: string | null,
  onChange?: (value: string | null) => void
  className?: string
}

export function FilterSeletor({items, placeholder, value, onChange, className}: parameters) {
  return (
    <Select items={items} value={value} onValueChange={onChange}>
      <SelectTrigger className={cn("w-45", className)}>
        <SelectValue placeholder={placeholder} />
      </SelectTrigger>
      <SelectContent  className="max-h-100 w-fit min-w-(--anchor-width) max-w-(--available-width)">
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
