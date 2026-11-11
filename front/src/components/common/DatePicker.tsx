import { format } from "date-fns";
import { es } from "date-fns/locale";

import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Field, FieldLabel } from "@/components/ui/field";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";

interface DatePickerProps {
  id: string;
  label: string;
  value: Date | undefined;
  onChange: (date: Date | undefined) => void;
  placeholder?: string;
}

// Selector de fecha controlado (Popover + Calendar)
export function DatePicker({
  id,
  label,
  value,
  onChange,
  placeholder = "Elige una fecha",
}: DatePickerProps) {
  return (
    <Field className="w-44">
      <FieldLabel htmlFor={id}>{label}</FieldLabel>
      <Popover>
        <PopoverTrigger
          render={
            <Button variant="outline" id={id} className="justify-start font-normal">
              {value ? format(value, "PPP", { locale: es }) : <span>{placeholder}</span>}
            </Button>
          }
        />
        <PopoverContent className="w-auto p-0" align="start">
          <Calendar mode="single" selected={value} onSelect={onChange} defaultMonth={value} />
        </PopoverContent>
      </Popover>
    </Field>
  );
}
