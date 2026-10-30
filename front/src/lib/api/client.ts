import {config} from "@/lib/config"
import {getAccessToken} from "@/lib/auth/keycloak"

export async function apiFetch(path:string, init: RequestInit = {}) {
    const headers = new Headers(init.headers)
    headers.set("Authorization",`Bearer ${await getAccessToken()}`)
    return fetch(`${config.apiBaseUrl}${path}`, { ...init, headers })
}