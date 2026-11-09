// Iconos
import {ArrowBigLeftDashIcon} from "lucide-react"

// Parametro
interface param {
    navBack: () => void
}

// Componente
export function BackButton({navBack}: param) {
  return (
    <div
      className="absolute top-3 left-17 p-1 rounded-full  border shadow-black z-50"
      onClick={navBack}
    >
      <ArrowBigLeftDashIcon />
    </div>
  );
}
