import { Button } from "@/components/index.comp"
import { Link } from "@tanstack/react-router"



// FORBIDDEN
export function NotFound () {
    return(
        <div className="w-screen h-screen flex flex-col justify-center items-center">
            <h1>404 Algo ha salido mal</h1>
            <Button>
                <Link to="/">Volver</Link>
            </Button>
        </div>
    )
}