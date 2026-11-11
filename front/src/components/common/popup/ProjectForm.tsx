import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import type { ProjectInput } from "@/lib/api/types";

interface ProjectFormProps {
  title: string;
  subtitle: string;
  submitLabel: string;
  initial?: { name: string; description: string | null };
  isPending: boolean;
  onSubmit: (values: ProjectInput) => void;
  onCancel: () => void;
}

// Formulario compartido por "Nuevo proyecto" y "Editar proyecto"
export function ProjectForm({
  title,
  subtitle,
  submitLabel,
  initial,
  isPending,
  onSubmit,
  onCancel,
}: ProjectFormProps) {
  const [name, setName] = useState(initial?.name ?? "");
  const [description, setDescription] = useState(initial?.description ?? "");

  const canSubmit = name.trim().length > 0 && !isPending;

  const handleSubmit = () => {
    if (!canSubmit) return;
    onSubmit({ name: name.trim(), description: description.trim() || null });
  };

  return (
    <div className="flex flex-col gap-4 rounded-2xl p-4 max-w-2xl">
      <div className="flex flex-col gap-3">
        <h1 className="text-2xl font-bold">{title}</h1>
        <p>{subtitle}</p>
      </div>

      <div className="flex flex-col gap-4">
        <label className="space-y-2">
          <span>Nombre*</span>
          <Input placeholder="Nombre" value={name} onChange={(e) => setName(e.target.value)} />
        </label>
        <label className="space-y-2">
          <span>Descripción</span>
          <Input
            placeholder="Descripción"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </label>
      </div>

      <div className="flex justify-end gap-3">
        <Button variant="outline" onClick={onCancel}>
          Cancelar
        </Button>
        <Button disabled={!canSubmit} onClick={handleSubmit}>
          {submitLabel}
        </Button>
      </div>
    </div>
  );
}
