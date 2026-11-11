import { Button } from "@/components";
import { useState, useEffect } from "react";
import { Moon, Sun } from "lucide-react";

// boton
export function DarkToggle() {
  const [dark, setDark] = useState(false);
  useEffect(() => {
    document.documentElement.classList.toggle("dark", dark);
  });
  return (
    <div className="p-4">
      <Button
        variant="ghost"
        size="icon"
        className="text-muted-foreground hover:bg-muted hover:text-foreground"
        onClick={() => setDark(!dark)}
      >
        {dark ? <Moon /> : <Sun />}
      </Button>
    </div>
  );
}
