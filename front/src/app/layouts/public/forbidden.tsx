import { Button } from "@/components/index.comp"
import { Link } from "@tanstack/react-router"



// FORBIDDEN
export function Forbidden () {
    return(
        <div className="w-screen h-screen flex flex-col justify-center items-center">
            <h1>403 no tienes acceso</h1>
            <Button>
                <Link to="/">Volver</Link>
            </Button>
        </div>
    )
}