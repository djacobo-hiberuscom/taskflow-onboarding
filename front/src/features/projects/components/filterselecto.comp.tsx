import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

interface item {
  label: string,
  value: string
}

interface parameters {
  items: item[],
  placeholder: string,
  value: string | null,
  onChange: (value: string | null) => void
}

export function FilterSeletor({items, placeholder, value, onChange}: parameters) {
  return (
    <Select items={items} value={value} onValueChange={onChange}>
      <SelectTrigger className="w-45">
        <SelectValue placeholder={placeholder} />
      </SelectTrigger>
      <SelectContent>
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
