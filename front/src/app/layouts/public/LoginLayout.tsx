// Router
import { Link } from "@tanstack/react-router";

// Componentes
import {
  Button,
  Card,
  CardAction,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
  Input,
  Label,
  BackGroundPatern,
} from "@/components";

// Keycloak
import { login } from "@/lib/auth/keycloak";

// FEAT
export function LoginLayout() {
  return (
    <>
      <BackGroundPatern />
      <div className="relative z-10 flex justify-center items-center h-screen w-screen">
        <Card className="w-full max-w-sm">
          <CardHeader>
            <CardTitle>Inicia sesio con tu cuenta</CardTitle>
            <CardDescription>
              Introduce tu nombre para iniciar sesion con tu cuenta
            </CardDescription>
            <CardAction>
              <Button variant="link">
                <Link to="/register">Registrate</Link>
              </Button>
            </CardAction>
          </CardHeader>
          <CardContent>
            <form>
              <div className="flex flex-col gap-6">
                <div className="grid gap-2">
                  <Label htmlFor="email">Email</Label>
                  <Input
                    id="email"
                    type="email"
                    placeholder="m@example.com"
                    required
                  />
                </div>
                <div className="grid gap-2">
                  <div className="flex items-center">
                    <Label htmlFor="password">Constraseña</Label>
                    <a
                      href="#"
                      className="ml-auto inline-block text-sm underline-offset-4 hover:underline"
                    >
                      ¿Has olvidado tu contraseña?
                    </a>
                  </div>
                  <Input
                    id="password"
                    type="password"
                    required
                    placeholder="Contraseña"
                  />
                </div>
              </div>
            </form>
          </CardContent>
          <CardFooter className="flex-col gap-2">
            <Button type="submit" className="w-full" onClick={login}>
              Login
            </Button>
            <Button variant="outline" className="w-full">
              Login with Google
            </Button>
          </CardFooter>
        </Card>
      </div>
    </>
  );
}
