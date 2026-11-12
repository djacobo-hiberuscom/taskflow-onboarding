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
import { register } from "@/lib/auth/keycloak";

// Feat
export function RegisterLayout() {
  return (
    <>
      <BackGroundPatern />
      <div className="relative z-10 flex justify-center items-center h-screen w-screen">
        <Card className="w-full max-w-sm">
          <CardHeader>
            <CardTitle>Create una cuenta</CardTitle>
            <CardDescription>
              Introduce tu correo para crear una cuenta
            </CardDescription>
            <CardAction>
              <Button variant="link">
                <Link to="/login">¿Ya tienes una cuenta?</Link>
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
            <Button type="submit" className="w-full" onClick={register}>
              Register
            </Button>
            <Button variant="outline" className="w-full">
              Register with Google
            </Button>
          </CardFooter>
        </Card>
      </div>
    </>
  );
}
