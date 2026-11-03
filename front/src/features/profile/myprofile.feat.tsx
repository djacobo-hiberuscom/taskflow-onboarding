import { Button } from "#components/ui/button";
import { logout } from "@/lib/auth/keycloak";

export function MyProfileFeat () {
    return (
        <div>
            <Button onClick={logout}>LogOut</Button>
        </div>
        
    )
}