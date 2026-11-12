import { GridPattern } from "@/components/index";

export function BackGroundPatern() {
  return (
    <GridPattern
      className="min-h-128"
      accentColor="#4F46E5"
      color="#EDEDF5"
      seed={4}
      size={32}
      speed={1.25}
    />
  );
}
