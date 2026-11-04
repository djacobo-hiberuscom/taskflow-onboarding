import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

import {initKeycloak} from "@/lib/auth/keycloak.ts"

function render () {
  createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
}

initKeycloak().catch(console.error).finally(render)
