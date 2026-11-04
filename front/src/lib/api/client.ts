import {config} from "@/lib/config"
import {getAccessToken} from "@/lib/auth/keycloak"

// API ERROR
export class ApiError extends Error{
    constructor(public status: number, public problem?: unknown){
        super(`HTTP ${status}`)
    }
}


// FETCH
export async function apiFetch<T = void>(path:string, init: RequestInit = {}): Promise<T> {
    const headers = new Headers(init.headers)
    headers.set("Authorization",`Bearer ${await getAccessToken()}`)
    if (init.body) headers.set("Content-Type", "application/json")

    const res = await fetch(`${config.apiBaseUrl}${path}`, { ...init, headers })

    if (!res.ok) {
        const problem = await res.json().catch(() => undefined)
        throw new ApiError(res.status, problem)
    }
    if (res.status === 204) return undefined as T
    return res.json() as Promise<T>
    
}