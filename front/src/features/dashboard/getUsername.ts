import USUARIOS from "@/assets/tempdata/users.json"
const users = USUARIOS

export function getUsernameById (id:string) {
    return users.find((u) => u.id === id)?.display_name
}